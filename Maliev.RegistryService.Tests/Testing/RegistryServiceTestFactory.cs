using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.RegistryService.Application.SeedData;
using Maliev.RegistryService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Maliev.RegistryService.Tests.Testing;

/// <summary>
/// Integration test factory for RegistryService.
/// Provides PostgreSQL, Redis, and RabbitMQ containers with parallel startup.
/// </summary>
public class RegistryServiceTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestAudience = "https://registry.test";
    private const string TestIssuer = "https://issuer.registry.test";
    private static PostgreSqlContainer? _postgresContainer;
    private static RedisContainer? _redisContainer;
    private static RabbitMqContainer? _rabbitmqContainer;
    private static bool _containersStarted;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private readonly RSA _testRsa;
    private readonly string _testJwtSecret;

    public RegistryServiceTestFactory()
    {
        _testRsa = RSA.Create(2048);
        _testJwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Set environment variable EARLY so Program.cs picks it up during WebApplication.CreateBuilder
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("CORS__AllowedOrigins__0", "http://localhost:3000");
        Environment.SetEnvironmentVariable("CORS_ALLOWED_ORIGINS", "http://localhost:3000");
    }

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_containersStarted)
            {
                _postgresContainer = new PostgreSqlBuilder()
                    .Build();

                _redisContainer = new RedisBuilder()
                    .Build();

                _rabbitmqContainer = new RabbitMqBuilder()
                    .Build();

                // Start all containers in parallel
                await Task.WhenAll(
                    _postgresContainer.StartAsync(),
                    _redisContainer.StartAsync(),
                    _rabbitmqContainer.StartAsync()
                );

                // Ensure PostgreSQL is fully ready and accepting connections
                var postgresReady = false;
                var retryCount = 0;
                const int maxRetries = 60;
                while (!postgresReady && retryCount < maxRetries)
                {
                    try
                    {
                        await using var conn = new Npgsql.NpgsqlConnection(_postgresContainer.GetConnectionString());
                        await conn.OpenAsync();
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 1";
                        await cmd.ExecuteScalarAsync();
                        postgresReady = true;
                    }
                    catch
                    {
                        retryCount++;
                        await Task.Delay(1000);
                    }
                }

                if (!postgresReady)
                {
                    throw new InvalidOperationException("PostgreSQL Testcontainer failed to become ready after 60 seconds.");
                }

                // Set connection strings as environment variables AFTER containers start
                // This ensures they're available when Program.cs reads configuration
                Environment.SetEnvironmentVariable("ConnectionStrings:RegistryDbContext", _postgresContainer.GetConnectionString());
                Environment.SetEnvironmentVariable("ConnectionStrings:redis", _redisContainer.GetConnectionString());
                Environment.SetEnvironmentVariable("ConnectionStrings:rabbitmq", _rabbitmqContainer.GetConnectionString());

                // Wait for Redis to be ready
                using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString()))
                {
                    await connection.GetDatabase().PingAsync();
                }

                // Apply database migrations
                await ApplyMigrationsAsync();

                _containersStarted = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        // Set environment variables immediately after containers start (for non-web tests)
        // Note: CreateHost() will also set these for web tests
        Environment.SetEnvironmentVariable("ConnectionStrings:RegistryDbContext", _postgresContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:redis", _redisContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:rabbitmq", _rabbitmqContainer!.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync(); // Stop the Host
        // Static containers are NOT disposed here to allow reuse across tests
        _testRsa.Dispose();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null); // Cleanup
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure containers are started before creating host
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        // Export RSA public key for JWT validation in PEM format (then Base64 encoded for AddJwtAuthentication)
        var publicKeyPem = _testRsa.ExportSubjectPublicKeyInfoPem();
        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        Environment.SetEnvironmentVariable("Jwt__PublicKey", publicKeyBase64);
        Environment.SetEnvironmentVariable("Jwt:PublicKey", publicKeyBase64);

        // Set connection strings as environment variables BEFORE creating host
        // This ensures they're available when Program.cs calls AddPostgresDbContext
        Environment.SetEnvironmentVariable("ConnectionStrings:RegistryDbContext", _postgresContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:redis", _redisContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:rabbitmq", _rabbitmqContainer!.GetConnectionString());

        // Also configure directly in the builder to ensure it's picked up
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RegistryDbContext"] = _postgresContainer!.GetConnectionString(),
                ["ConnectionStrings:redis"] = _redisContainer!.GetConnectionString(),
                ["ConnectionStrings:rabbitmq"] = _rabbitmqContainer!.GetConnectionString()
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure ASPNETCORE_ENVIRONMENT is set for all test hosts (including WithWebHostBuilder children)
        builder.UseSetting("ENVIRONMENT", "Testing");

        // Wait for containers to be ready before configuring the host
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Get public key for configuration
            var publicKeyPem = _testRsa.ExportSubjectPublicKeyInfoPem();
            var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

            // Read connection strings from environment variables (set in InitializeAsync)
            var connStr = Environment.GetEnvironmentVariable("ConnectionStrings:RegistryDbContext");
            var redisConn = Environment.GetEnvironmentVariable("ConnectionStrings:redis");
            var rabbitConn = Environment.GetEnvironmentVariable("ConnectionStrings:rabbitmq");

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecurityKey"] = _testJwtSecret,
                ["Jwt:PublicKey"] = publicKeyBase64,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["ServiceAuthentication:ClientId"] = "service-registry-service",
                ["ServiceAuthentication:ClientSecret"] = _testJwtSecret,
                ["Services:AuthService:BaseUrl"] = "https://auth.test",
                ["Services:IAMService:BaseUrl"] = "https://iam.test",
                ["CORS:AllowedOrigins:0"] = "http://localhost:3000",
                ["CORS_ALLOWED_ORIGINS"] = "http://localhost:3000",
                // Connection strings from environment variables (set after containers start)
                ["ConnectionStrings:RegistryDbContext"] = connStr,
                ["ConnectionStrings:redis"] = redisConn,
                ["ConnectionStrings:rabbitmq"] = rabbitConn
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Override the DbContext with the test container connection string
            var connStr = _postgresContainer!.GetConnectionString();
            services.AddDbContext<RegistryDbContext>(options =>
                options.UseNpgsql(connStr));

            // Configure JWT Bearer authentication with test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });

            // Add MassTransit test harness for testing message publishing/consuming
            services.AddMassTransitTestHarness();

            // Mock IAM service client to check permissions against JWT claims in integration tests
            services.AddScoped<IIamServiceClient>(sp =>
            {
                var mockIam = new Mock<IIamServiceClient>();
                mockIam.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false); // Return false to force fallback to JWT claims in tests
                return mockIam.Object;
            });
        });
    }

    /// <summary>
    /// Gets the DbContext from the service provider for use in tests.
    /// </summary>
    public RegistryDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
    }

    /// <summary>
    /// Creates a new DbContext instance for testing (not from DI container).
    /// </summary>
    public RegistryDbContext CreateDbContext()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<RegistryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new RegistryDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Applies all pending migrations to the test database.
    /// </summary>
    private async Task ApplyMigrationsAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();

        // Seed test data if database is empty
        if (!await context.ThaiLocations.AnyAsync())
        {
            var locations = ThaiLocationData.GetLocations();
            await context.ThaiLocations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Cleans all data from the database while preserving schema.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Get all table names from information_schema
        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                @"SELECT table_name
                  FROM information_schema.tables
                  WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name != '__EFMigrationsHistory'
                  ORDER BY table_name")
            .ToListAsync();

        // Truncate all tables (CASCADE handles foreign keys)
        foreach (var tableName in tableNames)
        {
            try
            {
#pragma warning disable EF1002 // SQL injection risk in test data cleaning
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
#pragma warning restore EF1002
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Table doesn't exist - ignore this error
            }
        }
    }

    /// <summary>
    /// Creates a test JWT token for authentication in integration tests.
    /// </summary>
    /// <param name="userId">User ID to include in token</param>
    /// <param name="roles">Roles to include in token claims</param>
    /// <param name="permissions">Permissions to include in token claims</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(
        string userId = "test-user",
        string[]? roles = null,
        string[]? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var rsaSecurityKey = new RsaSecurityKey(_testRsa);
        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an HTTP client with authenticated user and specified roles or permissions.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, string[]? permissions = null)
    {
        var token = CreateTestJwtToken(userId, roles, permissions: permissions);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}
