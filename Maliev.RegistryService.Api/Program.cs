using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Services;

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
    builder.AddRedisDistributedCache(instanceName: "registry:");
    builder.Services.AddMemoryCache();

    // MassTransit with RabbitMQ
    builder.AddMassTransitWithRabbitMq();

    // --- API Configuration ---
    builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader
    builder.Services.AddResponseCaching();

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Registry Service API",
            description: "Thai business registry and location data service. Provides Thai administrative divisions (provinces, districts, subdistricts), postal codes, DBD company lookups, and address autocomplete functionality.");
    }

    // Add Domain Services
    builder.Services.AddScoped<IThaiRegistryService, ThaiRegistryService>();
    builder.Services.AddHttpClient<IDbdProxyService, DbdProxyService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer()
    });

    // IAM Registration
    builder.AddIAMServiceClient("registry");

    builder.Services.AddControllers();

    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // Run database migrations on startup
    await app.MigrateDatabaseAsync<RegistryDbContext>();

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
