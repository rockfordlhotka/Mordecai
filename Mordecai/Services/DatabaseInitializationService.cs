using Microsoft.EntityFrameworkCore;
using Mordecai.Data;

namespace Mordecai.Services;

public class DatabaseInitializationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(IServiceProvider serviceProvider, ILogger<DatabaseInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();

        try
        {
            _logger.LogInformation("Initializing database...");
            
            // Ensure the database is created
            var created = await context.Database.EnsureCreatedAsync();
            
            if (created)
            {
                _logger.LogInformation("Database created successfully with seed data.");
            }
            else
            {
                _logger.LogInformation("Database already exists.");
            }

            // Check if we have the expected seed data
            var roomCount = await context.Rooms.CountAsync();
            var itemCount = await context.Items.CountAsync();
            
            _logger.LogInformation("Database initialized - Rooms: {RoomCount}, Items: {ItemCount}", roomCount, itemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();

        try
        {
            _logger.LogWarning("Resetting database - all data will be lost!");
            
            // Delete the database
            await context.Database.EnsureDeletedAsync();
            _logger.LogInformation("Database deleted successfully.");
            
            // Recreate the database with seed data
            await context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Database recreated successfully with fresh seed data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resetting the database.");
            throw;
        }
    }

    public async Task<DatabaseStatus> GetDatabaseStatusAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();

        try
        {
            var status = new DatabaseStatus
            {
                IsConnected = await context.Database.CanConnectAsync(),
                PlayerCount = await context.Players.CountAsync(),
                OnlinePlayerCount = await context.Players.CountAsync(p => p.IsOnline),
                RoomCount = await context.Rooms.CountAsync(),
                ItemCount = await context.Items.CountAsync(),
                ChatMessageCount = await context.ChatMessages.CountAsync(),
                ActiveSessionCount = await context.GameSessions.CountAsync(s => s.IsActive)
            };

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database status.");
            return new DatabaseStatus { IsConnected = false };
        }
    }
}

public class DatabaseStatus
{
    public bool IsConnected { get; set; } = false;
    public int PlayerCount { get; set; } = 0;
    public int OnlinePlayerCount { get; set; } = 0;
    public int RoomCount { get; set; } = 0;
    public int ItemCount { get; set; } = 0;
    public int ChatMessageCount { get; set; } = 0;
    public int ActiveSessionCount { get; set; } = 0;
}