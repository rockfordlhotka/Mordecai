using System.Collections.Concurrent;
using Mordecai.Data;
using Microsoft.EntityFrameworkCore;

namespace Mordecai.Services;

public class ChatMessage
{
    public ChatMessageType Type { get; set; }
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string PlayerName { get; set; } = "";
    public string? ConnectionId { get; set; }
}

public enum ChatMessageType
{
    Chat,           // Regular chat messages
    System,         // System notifications (join/leave)
    UserCommand,    // Player commands (echoed)
    GameResponse,   // Game responses to commands
    Description,    // Room/world descriptions
    Action,         // Action confirmations
    Error          // Error messages
}

public class ChatService
{
    private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new();
    private readonly List<ChatMessage> _chatHistory = new();
    private readonly object _historyLock = new();
    private readonly IServiceProvider _serviceProvider;
    
    public event Func<ChatMessage, Task>? MessageReceived;
    public event Func<string, Task>? PlayerJoined;
    public event Func<string, Task>? PlayerLeft;

    public ChatService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string> ConnectPlayerAsync(string playerName, Func<ChatMessage, Task> onMessageReceived)
    {
        var connectionId = Guid.NewGuid().ToString();
        var connection = new PlayerConnection(connectionId, playerName, onMessageReceived);
        
        _connections.TryAdd(connectionId, connection);

        // Update or create player in database
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
        
        try
        {
            var player = await context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
            if (player == null)
            {
                player = new Player
                {
                    Name = playerName,
                    CreatedAt = DateTime.UtcNow,
                    CurrentRoomId = 1, // Start in dungeon entrance
                    IsOnline = true
                };
                context.Players.Add(player);
                
                // Save the player first to get the ID
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Created new player: {playerName} with ID: {player.Id}");
            }
            else
            {
                player.IsOnline = true;
                player.LastLoginAt = DateTime.UtcNow;
                
                // Save player updates
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Updated existing player: {playerName} with ID: {player.Id}");
            }

            // Now create game session with the valid player ID
            var session = new GameSession
            {
                PlayerId = player.Id,
                ConnectionId = connectionId,
                StartedAt = DateTime.UtcNow,
                EndedAt = null, // Explicitly set to null for active sessions
                IsActive = true
            };
            context.GameSessions.Add(session);
            
            // Save the session
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Created game session for player {playerName} (ID: {player.Id}) with connection: {connectionId}");
            
            // Send chat history to the new player
            lock (_historyLock)
            {
                foreach (var message in _chatHistory.TakeLast(50)) // Send last 50 messages
                {
                    _ = Task.Run(() => onMessageReceived(message));
                }
            }
            
            // Notify others that player joined
            var joinMessage = new ChatMessage
            {
                Type = ChatMessageType.System,
                Content = $"{playerName} has entered the dungeon.",
                Timestamp = DateTime.Now,
                PlayerName = "System"
            };
            
            await BroadcastMessageAsync(joinMessage, excludeConnectionId: connectionId);
            await SaveMessageToDatabaseAsync(joinMessage, player.Id, player.CurrentRoomId);
            PlayerJoined?.Invoke(playerName);
            
            return connectionId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting player {playerName}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task DisconnectPlayerAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            // Update database
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
            
            var player = await context.Players.FirstOrDefaultAsync(p => p.Name == connection.PlayerName);
            if (player != null)
            {
                player.IsOnline = false;
                
                var session = await context.GameSessions
                    .FirstOrDefaultAsync(s => s.ConnectionId == connectionId && s.IsActive);
                if (session != null)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                }
                
                await context.SaveChangesAsync();
            }
            
            var leaveMessage = new ChatMessage
            {
                Type = ChatMessageType.System,
                Content = $"{connection.PlayerName} has left the dungeon.",
                Timestamp = DateTime.Now,
                PlayerName = "System"
            };
            
            await BroadcastMessageAsync(leaveMessage, excludeConnectionId: connectionId);
            await SaveMessageToDatabaseAsync(leaveMessage, player?.Id, player?.CurrentRoomId);
            PlayerLeft?.Invoke(connection.PlayerName);
        }
    }

    public async Task SendMessageAsync(string connectionId, string content, ChatMessageType type = ChatMessageType.Chat)
    {
        if (!_connections.TryGetValue(connectionId, out var connection))
            return;

        var message = new ChatMessage
        {
            Type = type,
            Content = content,
            Timestamp = DateTime.Now,
            PlayerName = connection.PlayerName,
            ConnectionId = connectionId
        };

        // Add to history
        lock (_historyLock)
        {
            _chatHistory.Add(message);
            
            // Keep only last 1000 messages
            if (_chatHistory.Count > 1000)
            {
                _chatHistory.RemoveAt(0);
            }
        }

        // Save to database
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
        var player = await context.Players.FirstOrDefaultAsync(p => p.Name == connection.PlayerName);
        await SaveMessageToDatabaseAsync(message, player?.Id, player?.CurrentRoomId);

        // Broadcast to all connected players
        await BroadcastMessageAsync(message);
        MessageReceived?.Invoke(message);
    }

    public async Task SendGameActionAsync(string connectionId, string action, string result)
    {
        if (!_connections.TryGetValue(connectionId, out var connection))
            return;

        // Send the action as a user command - only to the player who executed it
        var actionMessage = new ChatMessage
        {
            Type = ChatMessageType.UserCommand,
            Content = $"> {action}",
            Timestamp = DateTime.Now,
            PlayerName = connection.PlayerName,
            ConnectionId = connectionId
        };

        // Send the result as a game response - only to the player who executed it
        var resultMessage = new ChatMessage
        {
            Type = ChatMessageType.GameResponse,
            Content = result,
            Timestamp = DateTime.Now.AddMilliseconds(100),
            PlayerName = "Game",
            ConnectionId = connectionId
        };

        // Save to database
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
        var player = await context.Players.FirstOrDefaultAsync(p => p.Name == connection.PlayerName);
        
        await SaveMessageToDatabaseAsync(actionMessage, player?.Id, player?.CurrentRoomId);
        await SaveMessageToDatabaseAsync(resultMessage, player?.Id, player?.CurrentRoomId);

        // Send both messages only to the specific player (not broadcast)
        await SendMessageToPlayerAsync(connectionId, actionMessage);
        await SendMessageToPlayerAsync(connectionId, resultMessage);
    }

    private async Task SendMessageToPlayerAsync(string connectionId, ChatMessage message)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            await Task.Run(() => connection.OnMessageReceived(message));
        }
    }

    public List<string> GetConnectedPlayers()
    {
        return _connections.Values.Select(c => c.PlayerName).ToList();
    }

    private async Task BroadcastMessageAsync(ChatMessage message, string? excludeConnectionId = null)
    {
        var tasks = new List<Task>();
        
        foreach (var connection in _connections.Values)
        {
            if (connection.ConnectionId != excludeConnectionId)
            {
                tasks.Add(Task.Run(() => connection.OnMessageReceived(message)));
            }
        }
        
        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task SaveMessageToDatabaseAsync(ChatMessage message, int? playerId, int? roomId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
        
        var dbMessage = new Data.ChatMessage
        {
            Type = (Data.ChatMessageType)message.Type,
            Content = message.Content,
            Timestamp = message.Timestamp,
            PlayerName = message.PlayerName,
            ConnectionId = message.ConnectionId,
            PlayerId = playerId,
            RoomId = roomId
        };
        
        context.ChatMessages.Add(dbMessage);
        await context.SaveChangesAsync();
    }

    private record PlayerConnection(string ConnectionId, string PlayerName, Func<ChatMessage, Task> OnMessageReceived);
}
