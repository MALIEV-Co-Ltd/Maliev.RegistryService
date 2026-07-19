using Maliev.RegistryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.RegistryService.Tests.Integration;

public class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .Build();

    protected RegistryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        return new RegistryDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
