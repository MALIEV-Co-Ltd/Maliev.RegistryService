using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Services;
using Maliev.Aspire.ServiceDefaults;
using Maliev.RegistryService.Api;
using Microsoft.EntityFrameworkCore;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Log.StartingHost(bootstrapLogger, "Registry Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });

    builder.AddPostgresDbContext<RegistryDbContext>(connectionName: "RegistryDbContext"); // PostgreSQL with retry logic

    // Add Cache Service (standardized via ServiceDefaults)
    builder.AddStandardCache("registry:"); // Redis + in-memory fallback, memory-optimized
    builder.Services.AddMemoryCache();

    // MassTransit with RabbitMQ
    builder.AddMassTransitWithRabbitMq();

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader
    builder.Services.AddResponseCaching();

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // --- Authorization & Permissions ---
    builder.Services.AddPermissionAuthorization();

    // Add Domain Services
    builder.Services.AddScoped<IThaiRegistryService, ThaiRegistryService>();

    // Configure BDEX API options from user secrets
    builder.Services.Configure<Maliev.RegistryService.Data.Configuration.BdexApiOptions>(
        builder.Configuration.GetSection(Maliev.RegistryService.Data.Configuration.BdexApiOptions.SectionName));

    // Add HttpClient for BDEX API (api.dbd.go.th)
    builder.Services.AddHttpClient<IDbdProxyService, DbdProxyService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        };
        return handler;
    })
    .AddStandardResilienceHandler(); // Standard retry, circuit breaker, and timeout policies
    
    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Registry Service API",
            description: "Thai business registry and location data service. Provides Thai administrative divisions (provinces, districts, subdistricts), postal codes, DBD company lookups, and address autocomplete functionality.");
    }

    // IAM Registration
    builder.AddIAMServiceClient(RegistryConstants.ServiceName);
    builder.Services.AddIAMRegistration<Maliev.RegistryService.Api.Services.RegistryIAMRegistrationService>(RegistryConstants.ServiceName);

    builder.Services.AddControllers();

    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // Run database migrations on startup
    await app.MigrateDatabaseAsync<RegistryDbContext>();

    // Seed production location data on startup
    if (app.Environment.IsDevelopment())
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
            
            if (!await context.ThaiLocations.AnyAsync() || args.Contains("--seed"))
            {
                logger.LogInformation("Seeding Thai locations from SQL file...");
                var sqlPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "thai_locations.sql");
                if (!File.Exists(sqlPath)) 
                {
                    sqlPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Maliev.RegistryService.Data", "SeedData", "thai_locations.sql");
                }
                
                if (File.Exists(sqlPath))
                {
                    var sql = await File.ReadAllTextAsync(sqlPath);
                    await context.Database.ExecuteSqlRawAsync(sql);
                    logger.LogInformation("Thai locations seeded successfully.");
                }
                else
                {
                    logger.LogWarning("Seed data file not found at {Path}", sqlPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed database");
        }
    }

    app.UseStandardMiddleware();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors();

    // JWT Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseResponseCaching();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "registry");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "registry");

    Log.ServiceStarted(logger, "Registry Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.HostTerminated(bootstrapLogger, ex, "Registry Service");
    // Force flush to ensure Aspire captures the error before process exits
    Console.Out.Flush();
    Console.Error.Flush();
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main entry point for the Maliev Registry Service API.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}
// Trigger CI
