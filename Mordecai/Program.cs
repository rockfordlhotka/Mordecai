using Mordecai.Components;
using Mordecai.Services;
using Mordecai.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging for better visibility of background services
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<MordecaiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                     "Data Source=mordecai.db"));

// Add services
builder.Services.AddSingleton<ChatService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<DatabaseInitializationService>();

// Add background services
builder.Services.AddHostedService<AtmosphereService>();

var app = builder.Build();

// Initialize database with proper error handling
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await dbInitService.InitializeAsync();
        
        // Log database status for debugging
        var status = await dbInitService.GetDatabaseStatusAsync();
        logger.LogInformation("Database Status - Connected: {IsConnected}, Rooms: {RoomCount}, Items: {ItemCount}", 
            status.IsConnected, status.RoomCount, status.ItemCount);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed. You may need to delete the mordecai.db file and restart the application.");
        
        // Optionally, you can automatically delete and recreate the database
        var context = scope.ServiceProvider.GetRequiredService<MordecaiDbContext>();
        logger.LogWarning("Attempting to delete and recreate database...");
        
        try
        {
            await context.Database.EnsureDeletedAsync();
            logger.LogInformation("Database deleted successfully.");
            
            await dbInitService.InitializeAsync();
            logger.LogInformation("Database recreated successfully.");
        }
        catch (Exception recreateEx)
        {
            logger.LogError(recreateEx, "Failed to recreate database. Please manually delete mordecai.db file and restart.");
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
