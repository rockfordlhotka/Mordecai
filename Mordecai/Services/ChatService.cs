using System.Collections.Concurrent;

namespace Mordecai.Services;

public class ChatService
{
    private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new();
    private readonly List<ChatMessage> _chatHistory = new();
    private readonly object _historyLock = new();
    
    public event Func<ChatMessage, Task>? MessageReceived;
    public event Func<string, Task>? PlayerJoined;
    public event Func<string, Task>? PlayerLeft;

    public async Task<string> ConnectPlayerAsync(string playerName, Func<ChatMessage, Task> onMessageReceived)
    {
        var connectionId = Guid.NewGuid().ToString();
        var connection = new PlayerConnection(connectionId, playerName, onMessageReceived);
        
        _connections.TryAdd(connectionId, connection);
        
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
        PlayerJoined?.Invoke(playerName);
        
        return connectionId;
    }

    public async Task DisconnectPlayerAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            var leaveMessage = new ChatMessage
            {
                Type = ChatMessageType.System,
                Content = $"{connection.PlayerName} has left the dungeon.",
                Timestamp = DateTime.Now,
                PlayerName = "System"
            };
            
            await BroadcastMessageAsync(leaveMessage, excludeConnectionId: connectionId);
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

    private record PlayerConnection(string ConnectionId, string PlayerName, Func<ChatMessage, Task> OnMessageReceived);
}

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
