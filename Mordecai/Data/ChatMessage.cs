using System.ComponentModel.DataAnnotations;

namespace Mordecai.Data;

public class ChatMessage
{
    public int Id { get; set; }
    
    public ChatMessageType Type { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = "";
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(50)]
    public string PlayerName { get; set; } = "";
    
    [MaxLength(100)]
    public string? ConnectionId { get; set; }
    
    // Foreign key relationships
    public int? PlayerId { get; set; }
    public Player? Player { get; set; }
    
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
}

public enum ChatMessageType
{
    Chat,           // Regular chat messages
    System,         // System notifications (join/leave)
    UserCommand,    // Player commands (echoed)
    GameResponse,   // Game responses to commands
    Description,    // Room/world descriptions
    Action,         // Action confirmations
    Error,          // Error messages
    Atmosphere      // Atmospheric/ambient messages (thunder, etc.)
}