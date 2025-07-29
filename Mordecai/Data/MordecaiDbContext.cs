using Microsoft.EntityFrameworkCore;

namespace Mordecai.Data;

public class MordecaiDbContext : DbContext
{
    public MordecaiDbContext(DbContextOptions<MordecaiDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<GameItem> Items { get; set; }
    public DbSet<PlayerItem> PlayerItems { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Player configuration
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastLoginAt);
            entity.Property(e => e.CurrentRoomId);
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.Experience).HasDefaultValue(0);
            entity.Property(e => e.Health).HasDefaultValue(100);
            entity.Property(e => e.MaxHealth).HasDefaultValue(100);
            entity.Property(e => e.IsOnline).HasDefaultValue(false);

            // Foreign key relationship
            entity.HasOne(e => e.CurrentRoom)
                  .WithMany(r => r.PlayersInRoom)
                  .HasForeignKey(e => e.CurrentRoomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ShortDescription).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // GameItem configuration
        modelBuilder.Entity<GameItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ItemType).IsRequired();
            entity.Property(e => e.Value).HasDefaultValue(0);
            entity.Property(e => e.Weight).HasDefaultValue(0.0);
            entity.Property(e => e.IsStackable).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // PlayerItem configuration (many-to-many relationship)
        modelBuilder.Entity<PlayerItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.AcquiredAt).IsRequired();

            entity.HasOne(e => e.Player)
                  .WithMany(p => p.PlayerItems)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Item)
                  .WithMany()
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.PlayerName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConnectionId).HasMaxLength(100);

            entity.HasOne(e => e.Player)
                  .WithMany()
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Room)
                  .WithMany()
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GameSession configuration
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConnectionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.EndedAt).IsRequired(false); // Explicitly allow null
            entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();

            entity.HasOne(e => e.Player)
                  .WithMany(p => p.GameSessions)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Use fixed dates for seeding to avoid issues with DateTime.UtcNow in seed data
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Seed initial rooms with more complex connections
        modelBuilder.Entity<Room>().HasData(
            new Room
            {
                Id = 1,
                Name = "Dungeon Entrance",
                Description = "You stand at the entrance of a dark, mysterious dungeon. Ancient stone walls stretch before you, carved with strange runes that seem to glow faintly in the dim light. To the north, a narrow corridor disappears into darkness.",
                ShortDescription = "The entrance to Mordecai's dungeon",
                CreatedAt = baseDate,
                NorthRoomId = 2
            },
            new Room
            {
                Id = 2,
                Name = "Dark Corridor",
                Description = "A dimly lit stone corridor stretches before you. The walls are damp and covered in moss. Strange shadows dance in the flickering torchlight. You can hear the distant sound of dripping water echoing through the passages.",
                ShortDescription = "A dark, moss-covered corridor",
                CreatedAt = baseDate.AddHours(1),
                SouthRoomId = 1,
                NorthRoomId = 3,
                EastRoomId = 4
            },
            new Room
            {
                Id = 3,
                Name = "Chamber of Echoes",
                Description = "You enter a vast circular chamber with a high vaulted ceiling. Your footsteps echo endlessly in the darkness above. Ancient pillars carved with mystical symbols support the ceiling, and a faint magical energy seems to emanate from the stone itself.",
                ShortDescription = "A vast chamber with echoing footsteps",
                CreatedAt = baseDate.AddHours(2),
                SouthRoomId = 2,
                WestRoomId = 5
            },
            new Room
            {
                Id = 4,
                Name = "The Armory",
                Description = "You find yourself in what appears to be an old armory. Rusty weapon racks line the walls, most empty but a few still holding ancient swords and shields. The air smells of old metal and leather. Dust motes dance in the thin shafts of light filtering through cracks in the ceiling.",
                ShortDescription = "An abandoned armory with old weapons",
                CreatedAt = baseDate.AddHours(3),
                WestRoomId = 2,
                NorthRoomId = 6
            },
            new Room
            {
                Id = 5,
                Name = "Crystal Cavern",
                Description = "This natural cavern is filled with glowing crystals that cast rainbow reflections on the walls. The crystals hum with a low, musical tone that seems to resonate in your bones. Small pools of clear water reflect the crystal light, creating an otherworldly atmosphere.",
                ShortDescription = "A magical cavern filled with glowing crystals",
                CreatedAt = baseDate.AddHours(4),
                EastRoomId = 3,
                NorthRoomId = 7
            },
            new Room
            {
                Id = 6,
                Name = "Guard Tower",
                Description = "You climb a spiral stone staircase to reach this watchtower. Arrow slits provide narrow views of the surrounding dungeon passages below. Old torches hang in iron sconces, and you can see worn grooves in the stone floor where guards once paced back and forth.",
                ShortDescription = "A stone watchtower overlooking the dungeon",
                CreatedAt = baseDate.AddHours(5),
                SouthRoomId = 4
            },
            new Room
            {
                Id = 7,
                Name = "The Underground Lake",
                Description = "You emerge onto the shore of a vast underground lake. The water is perfectly still and black as obsidian, reflecting the ceiling like a dark mirror. You can hear the gentle lapping of water against the rocky shore, and somewhere in the distance, the sound of a waterfall.",
                ShortDescription = "The shore of a mysterious underground lake",
                CreatedAt = baseDate.AddHours(6),
                SouthRoomId = 5
            }
        );

        // Seed initial items
        modelBuilder.Entity<GameItem>().HasData(
            new GameItem
            {
                Id = 1,
                Name = "Rusty Sword",
                Description = "An old iron sword with a rusty blade. Despite its worn appearance, it still feels sturdy in your hands.",
                ItemType = ItemType.Weapon,
                Value = 10,
                Weight = 3.5,
                IsStackable = false,
                CreatedAt = baseDate
            },
            new GameItem
            {
                Id = 2,
                Name = "Small Health Potion",
                Description = "A small glass vial containing a red liquid that glows softly. It smells of herbs and magic.",
                ItemType = ItemType.Consumable,
                Value = 25,
                Weight = 0.2,
                IsStackable = true,
                CreatedAt = baseDate
            },
            new GameItem
            {
                Id = 3,
                Name = "Torch",
                Description = "A wooden torch wrapped with oil-soaked cloth. It provides a warm, flickering light in the darkness.",
                ItemType = ItemType.Tool,
                Value = 5,
                Weight = 1.0,
                IsStackable = true,
                CreatedAt = baseDate
            },
            new GameItem
            {
                Id = 4,
                Name = "Crystal Shard",
                Description = "A small, glowing crystal shard that pulses with inner light. It feels warm to the touch and seems to respond to your emotions.",
                ItemType = ItemType.Treasure,
                Value = 50,
                Weight = 0.1,
                IsStackable = true,
                CreatedAt = baseDate
            },
            new GameItem
            {
                Id = 5,
                Name = "Ancient Shield",
                Description = "A heavy iron shield bearing the crest of some long-forgotten kingdom. Despite its age, it still provides excellent protection.",
                ItemType = ItemType.Armor,
                Value = 35,
                Weight = 8.0,
                IsStackable = false,
                CreatedAt = baseDate
            }
        );
    }
}