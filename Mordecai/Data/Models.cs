using System.ComponentModel.DataAnnotations;

namespace Mordecai.Data;

public class Player
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = "";
    
    [MaxLength(255)]
    public string? Email { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Character stats
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    
    // Current location
    public int? CurrentRoomId { get; set; }
    public Room? CurrentRoom { get; set; }
    
    // Online status
    public bool IsOnline { get; set; } = false;
    
    // Navigation properties
    public ICollection<PlayerItem> PlayerItems { get; set; } = new List<PlayerItem>();
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}

public class Room
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";
    
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = "";
    
    [MaxLength(200)]
    public string? ShortDescription { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Room connections (can be expanded later for complex room relationships)
    public int? NorthRoomId { get; set; }
    public int? SouthRoomId { get; set; }
    public int? EastRoomId { get; set; }
    public int? WestRoomId { get; set; }
    
    // Navigation properties
    public ICollection<Player> PlayersInRoom { get; set; } = new List<Player>();
}

public class GameItem
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = "";
    
    public ItemType ItemType { get; set; }
    public int Value { get; set; } = 0;
    public double Weight { get; set; } = 0.0;
    public bool IsStackable { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PlayerItem
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    public int ItemId { get; set; }
    public GameItem Item { get; set; } = null!;
    
    public int Quantity { get; set; } = 1;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}

public class GameSession
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string ConnectionId { get; set; } = "";
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Tool,
    Treasure,
    Quest,
    Misc
}