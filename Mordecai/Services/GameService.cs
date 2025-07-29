using Microsoft.EntityFrameworkCore;
using Mordecai.Data;

namespace Mordecai.Services;

public class GameService
{
    private readonly MordecaiDbContext _context;

    public GameService(MordecaiDbContext context)
    {
        _context = context;
    }

    // Player management
    public async Task<Player?> GetPlayerByNameAsync(string name)
    {
        return await _context.Players
            .Include(p => p.CurrentRoom)
            .Include(p => p.PlayerItems)
                .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<Player> CreatePlayerAsync(string name, string? email = null)
    {
        var player = new Player
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            CurrentRoomId = 1, // Start in dungeon entrance
            Level = 1,
            Experience = 0,
            Health = 100,
            MaxHealth = 100
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        // Give starting items
        await GivePlayerStartingItemsAsync(player.Id);

        return player;
    }

    public async Task UpdatePlayerLocationAsync(int playerId, int roomId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player != null)
        {
            var oldRoomId = player.CurrentRoomId;
            player.CurrentRoomId = roomId;
            await _context.SaveChangesAsync();
            
            // Add debug logging
            Console.WriteLine($"Player {playerId} moved from room {oldRoomId} to room {roomId}");
        }
    }

    // Room management
    public async Task<Room?> GetRoomAsync(int roomId)
    {
        return await _context.Rooms
            .Include(r => r.PlayersInRoom.Where(p => p.IsOnline))
            .FirstOrDefaultAsync(r => r.Id == roomId);
    }

    /// <summary>
    /// Gets room data with fresh player information, useful for ensuring consistency
    /// </summary>
    public async Task<Room?> GetRoomWithFreshDataAsync(int roomId)
    {
        // Clear any tracked entities to ensure fresh data
        _context.ChangeTracker.Clear();
        
        return await _context.Rooms
            .Include(r => r.PlayersInRoom.Where(p => p.IsOnline))
            .AsNoTracking() // Ensure no caching
            .FirstOrDefaultAsync(r => r.Id == roomId);
    }

    public async Task<Room?> GetRoomInDirectionAsync(int currentRoomId, string direction)
    {
        var room = await _context.Rooms.FindAsync(currentRoomId);
        if (room == null) return null;

        int? targetRoomId = direction.ToLower() switch
        {
            "north" => room.NorthRoomId,
            "south" => room.SouthRoomId,
            "east" => room.EastRoomId,
            "west" => room.WestRoomId,
            _ => null
        };

        if (targetRoomId.HasValue)
        {
            return await GetRoomAsync(targetRoomId.Value);
        }

        return null;
    }

    public async Task<List<Room>> GetAllRoomsAsync()
    {
        return await _context.Rooms.ToListAsync();
    }

    // Movement system
    public async Task<MovementResult> MovePlayerAsync(string playerName, string direction)
    {
        var player = await GetPlayerByNameAsync(playerName);
        if (player == null)
        {
            return new MovementResult { Success = false, Message = "Player not found." };
        }

        // Get fresh room data
        var currentRoom = await GetRoomAsync(player.CurrentRoomId ?? 1);
        if (currentRoom == null)
        {
            return new MovementResult { Success = false, Message = "Current location unknown." };
        }

        var targetRoom = await GetRoomInDirectionAsync(currentRoom.Id, direction);
        if (targetRoom == null)
        {
            var validDirections = GetValidDirections(currentRoom);
            var directionsText = validDirections.Any() 
                ? $"Valid directions are: {string.Join(", ", validDirections)}" 
                : "There are no exits from this room.";
                
            return new MovementResult 
            { 
                Success = false, 
                Message = $"You cannot go {direction} from here. {directionsText}" 
            };
        }

        // Get players currently in both rooms (before the move)
        var playersInCurrentRoom = currentRoom.PlayersInRoom?.Where(p => p.Name != playerName && p.IsOnline).ToList() ?? new List<Player>();
        var playersInTargetRoom = targetRoom.PlayersInRoom?.Where(p => p.IsOnline).ToList() ?? new List<Player>();

        // Move the player and ensure the transaction is completed
        await UpdatePlayerLocationAsync(player.Id, targetRoom.Id);

        // Add a small delay to ensure database consistency across different contexts
        await Task.Delay(50);

        // Get fresh room data after the move to ensure we have the updated state
        var refreshedTargetRoom = await GetRoomAsync(targetRoom.Id);
        var refreshedCurrentRoom = await GetRoomAsync(currentRoom.Id);

        return new MovementResult
        {
            Success = true,
            Message = $"You move {direction} to {targetRoom.Name}.",
            NewRoom = refreshedTargetRoom,
            PreviousRoom = refreshedCurrentRoom,
            PlayersInNewRoom = playersInTargetRoom,
            PlayersInPreviousRoom = playersInCurrentRoom
        };
    }

    public async Task<string> GetRoomDescriptionAsync(string playerName)
    {
        var player = await GetPlayerByNameAsync(playerName);
        if (player == null) return "You are nowhere.";

        // Use the fresh data method to ensure we see current players
        var room = await GetRoomWithFreshDataAsync(player.CurrentRoomId ?? 1);
        if (room == null) return "You are in an unknown location.";

        var description = room.Description;
        
        // Add information about available exits
        var validDirections = GetValidDirections(room);
        if (validDirections.Any())
        {
            description += $"\n\nObvious exits: {string.Join(", ", validDirections)}";
        }
        else
        {
            description += "\n\nThere are no obvious exits.";
        }

        // Get other players in the room with fresh data
        var otherPlayers = room.PlayersInRoom?
            .Where(p => p.Name != playerName && p.IsOnline)
            .ToList() ?? new List<Player>();
            
        if (otherPlayers.Any())
        {
            var playerNames = string.Join(", ", otherPlayers.Select(p => p.Name));
            description += $"\n\nOther players here: {playerNames}";
        }

        return description;
    }

    /// <summary>
    /// Allows a player to look in a specific direction to see information about
    /// the connected room without actually moving there. Shows the room name,
    /// short description, and any players currently in that room.
    /// </summary>
    /// <param name="playerName">Name of the player looking</param>
    /// <param name="direction">Direction to look (north, south, east, west)</param>
    /// <returns>Description of what the player can see in that direction</returns>
    public async Task<string> LookInDirectionAsync(string playerName, string direction)
    {
        var player = await GetPlayerByNameAsync(playerName);
        if (player == null) return "You are nowhere.";

        // Always get fresh room data
        var currentRoom = await GetRoomAsync(player.CurrentRoomId ?? 1);
        if (currentRoom == null) return "You are in an unknown location.";

        var targetRoom = await GetRoomInDirectionAsync(currentRoom.Id, direction);
        if (targetRoom == null)
        {
            return $"You cannot see anything to the {direction}.";
        }

        // Build the description with room name
        var description = $"Looking {direction}, you see: {targetRoom.Name}";
        
        // Add short description if available
        if (!string.IsNullOrWhiteSpace(targetRoom.ShortDescription))
        {
            description += $"\n{targetRoom.ShortDescription}";
        }

        // Always get fresh room data to see current players
        var refreshedTargetRoom = await GetRoomAsync(targetRoom.Id);
        var playersInRoom = refreshedTargetRoom?.PlayersInRoom?.Where(p => p.IsOnline).ToList() ?? new List<Player>();
        
        if (playersInRoom.Any())
        {
            var playerNames = string.Join(", ", playersInRoom.Select(p => p.Name));
            if (playersInRoom.Count == 1)
            {
                description += $"\nYou can see {playerNames} there.";
            }
            else
            {
                description += $"\nYou can see {playerNames} there.";
            }
        }

        return description;
    }

    private List<string> GetValidDirections(Room room)
    {
        var directions = new List<string>();
        
        if (room.NorthRoomId.HasValue) directions.Add("north");
        if (room.SouthRoomId.HasValue) directions.Add("south");
        if (room.EastRoomId.HasValue) directions.Add("east");
        if (room.WestRoomId.HasValue) directions.Add("west");
        
        return directions;
    }

    public async Task<string> GetPlayerInventoryAsync(string playerName)
    {
        var player = await GetPlayerByNameAsync(playerName);
        if (player == null) return "Player not found.";

        var inventory = await GetPlayerInventoryAsync(player.Id);
        if (!inventory.Any())
        {
            return "Your inventory is empty.";
        }

        var items = inventory.Select(pi => 
            pi.Item.IsStackable && pi.Quantity > 1 
                ? $"{pi.Item.Name} (x{pi.Quantity})" 
                : pi.Item.Name);

        return $"Your inventory: {string.Join(", ", items)}";
    }

    // Item management
    public async Task<List<PlayerItem>> GetPlayerInventoryAsync(int playerId)
    {
        return await _context.PlayerItems
            .Include(pi => pi.Item)
            .Where(pi => pi.PlayerId == playerId)
            .ToListAsync();
    }

    public async Task<bool> GiveItemToPlayerAsync(int playerId, int itemId, int quantity = 1)
    {
        var player = await _context.Players.FindAsync(playerId);
        var item = await _context.Items.FindAsync(itemId);
        
        if (player == null || item == null) return false;

        // Check if player already has this item and it's stackable
        var existingPlayerItem = await _context.PlayerItems
            .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == itemId);

        if (existingPlayerItem != null && item.IsStackable)
        {
            existingPlayerItem.Quantity += quantity;
        }
        else
        {
            var playerItem = new PlayerItem
            {
                PlayerId = playerId,
                ItemId = itemId,
                Quantity = quantity,
                AcquiredAt = DateTime.UtcNow
            };
            _context.PlayerItems.Add(playerItem);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task GivePlayerStartingItemsAsync(int playerId)
    {
        // Give starting items (rusty sword, health potion, torch)
        await GiveItemToPlayerAsync(playerId, 1); // Rusty Sword
        await GiveItemToPlayerAsync(playerId, 2); // Small Health Potion
        await GiveItemToPlayerAsync(playerId, 3); // Torch
    }

    // Game session management
    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .Include(gs => gs.Player)
            .Where(gs => gs.IsActive)
            .ToListAsync();
    }

    public async Task<GameSession?> GetActiveSessionByConnectionIdAsync(string connectionId)
    {
        return await _context.GameSessions
            .Include(gs => gs.Player)
            .FirstOrDefaultAsync(gs => gs.ConnectionId == connectionId && gs.IsActive);
    }

    // Chat history
    public async Task<List<Data.ChatMessage>> GetRecentChatMessagesAsync(int? roomId = null, int count = 50)
    {
        var query = _context.ChatMessages
            .Include(cm => cm.Player)
            .Include(cm => cm.Room)
            .OrderByDescending(cm => cm.Timestamp);

        if (roomId.HasValue)
        {
            query = (IOrderedQueryable<Data.ChatMessage>)query.Where(cm => cm.RoomId == roomId.Value);
        }

        return await query.Take(count).ToListAsync();
    }

    // Player statistics
    public async Task<int> GetTotalPlayersCountAsync()
    {
        return await _context.Players.CountAsync();
    }

    public async Task<int> GetOnlinePlayersCountAsync()
    {
        return await _context.Players.CountAsync(p => p.IsOnline);
    }

    public async Task<List<Player>> GetTopPlayersByLevelAsync(int count = 10)
    {
        return await _context.Players
            .OrderByDescending(p => p.Level)
            .ThenByDescending(p => p.Experience)
            .Take(count)
            .ToListAsync();
    }

    // Game world management
    public async Task<bool> PlayerExistsAsync(string name)
    {
        return await _context.Players.AnyAsync(p => p.Name == name);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class MovementResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Room? NewRoom { get; set; }
    public Room? PreviousRoom { get; set; }
    public List<Player> PlayersInNewRoom { get; set; } = new();
    public List<Player> PlayersInPreviousRoom { get; set; } = new();
}