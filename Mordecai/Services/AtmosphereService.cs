using Microsoft.Extensions.Hosting;
using Mordecai.Services;

namespace Mordecai.Services;

public class AtmosphereService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AtmosphereService> _logger;
    private readonly Random _random = new();
    
    // Thunder messages pool for variety
    private readonly string[] _thunderMessages = 
    [
        "You hear thunder rumbling in the distance.",
        "A low rumble of thunder echoes through the dungeon.",
        "Thunder booms somewhere far above the stone halls.",
        "The sound of distant thunder reverberates through the corridors.",
        "A deep, rolling thunder can be heard from beyond these walls.",
        "Thunder crashes in the distance, making the ground tremble slightly.",
        "You hear the ominous sound of thunder rolling across the sky.",
        "A thunderclap echoes through the dungeon, distant but powerful."
    ];

    public AtmosphereService(IServiceProvider serviceProvider, ILogger<AtmosphereService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Atmosphere Service started - thunder will occur approximately every 15 minutes");

        // For development/testing, start with a shorter initial delay
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (isDevelopment)
        {
            _logger.LogInformation("Development mode detected - first thunder will occur in 2 minutes for testing");
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            
            if (!stoppingToken.IsCancellationRequested)
            {
                await GenerateThunderAsync();
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a random interval around 15 minutes (10-20 minutes)
                var baseInterval = TimeSpan.FromMinutes(15);
                var randomVariation = TimeSpan.FromMinutes(_random.Next(-5, 6)); // ±5 minutes
                var nextThunderDelay = baseInterval + randomVariation;
                
                _logger.LogDebug("Next thunder scheduled in {Minutes} minutes", nextThunderDelay.TotalMinutes);

                await Task.Delay(nextThunderDelay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await GenerateThunderAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Atmosphere Service");
                
                // Wait a bit before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        _logger.LogInformation("Atmosphere Service stopped");
    }

    private async Task GenerateThunderAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();

            // Check if there are any connected players
            var connectedPlayers = chatService.GetConnectedPlayers();
            
            if (connectedPlayers.Count == 0)
            {
                _logger.LogDebug("No players connected, skipping thunder message");
                return;
            }

            // Select a random thunder message
            var thunderMessage = _thunderMessages[_random.Next(_thunderMessages.Length)];
            
            // Create the atmosphere message
            var message = new ChatMessage
            {
                Type = ChatMessageType.Atmosphere,
                Content = thunderMessage,
                Timestamp = DateTime.Now,
                PlayerName = "Nature"
            };

            // Broadcast to all connected players
            await chatService.BroadcastAtmosphereMessageAsync(message);
            
            _logger.LogInformation("Thunder message sent to {PlayerCount} players: {Message}", 
                connectedPlayers.Count, thunderMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thunder message");
        }
    }
}