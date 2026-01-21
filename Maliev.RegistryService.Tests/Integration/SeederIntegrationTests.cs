using Maliev.RegistryService.Data.Seeding;
using Maliev.RegistryService.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Integration;

public class SeederIntegrationTests : IClassFixture<RegistryServiceTestFactory>
{
    private readonly RegistryServiceTestFactory _factory;

    public SeederIntegrationTests(RegistryServiceTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_WhenDataExists_Skips()
    {
        // Arrange
        using var context = _factory.CreateDbContext();
        await _factory.CleanDatabaseAsync();
        context.ThaiLocations.Add(new() { PostalCode = "99999" });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ThaiLocationSeeder>>();
        var seeder = new ThaiLocationSeeder(context, loggerMock.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        Assert.Equal(1, await context.ThaiLocations.CountAsync());
    }
}
