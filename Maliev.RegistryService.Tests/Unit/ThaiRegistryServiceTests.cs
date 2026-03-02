using Maliev.RegistryService.Infrastructure.Services;
using Maliev.RegistryService.Infrastructure.Persistence;
using Maliev.RegistryService.Domain.Entities;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class ThaiRegistryServiceTests
{
    private readonly DbContextOptions<RegistryDbContext> _options;

    public ThaiRegistryServiceTests()
    {
        _options = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsLocation()
    {
        // Arrange
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "12345", SubDistrictEn = "Test", DistrictEn = "Test", ProvinceEn = "Test", SubDistrictTh = "Test", DistrictTh = "Test", ProvinceTh = "Test" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        // Act
        var result = await service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_SavesToDatabase()
    {
        // Arrange
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var location = new ThaiLocation { PostalCode = "55555", SubDistrictEn = "New", DistrictEn = "New", ProvinceEn = "New", SubDistrictTh = "New", DistrictTh = "New", ProvinceTh = "New" };

        // Act
        var result = await service.CreateAsync(location);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await context.ThaiLocations.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("55555", saved.PostalCode);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingData()
    {
        // Arrange
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "10000", SubDistrictEn = "Old", DistrictEn = "Old", ProvinceEn = "Old", SubDistrictTh = "Old", DistrictTh = "Old", ProvinceTh = "Old" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);
        var updated = new ThaiLocation { Id = id, PostalCode = "20000", SubDistrictEn = "New", DistrictEn = "New", ProvinceEn = "New", SubDistrictTh = "New", DistrictTh = "New", ProvinceTh = "New" };

        // Act
        var success = await service.UpdateAsync(updated);

        // Assert
        Assert.True(success);
        var saved = await context.ThaiLocations.FindAsync(id);
        Assert.Equal("20000", saved!.PostalCode);
        Assert.Equal("New", saved.SubDistrictEn);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFromDatabase()
    {
        // Arrange
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "10000", SubDistrictEn = "Old", DistrictEn = "Old", ProvinceEn = "Old", SubDistrictTh = "Old", DistrictTh = "Old", ProvinceTh = "Old" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        // Act
        var success = await service.DeleteAsync(id);

        // Assert
        Assert.True(success);
        var saved = await context.ThaiLocations.FindAsync(id);
        Assert.Null(saved);
    }

    [Fact]
    public async Task AutocompleteAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);

        // Act
        var result = await service.AutocompleteAsync("", 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var result = await service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsFalse()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var missing = new ThaiLocation
        {
            Id = Guid.NewGuid(),
            PostalCode = "00000",
            SubDistrictEn = "X",
            DistrictEn = "X",
            ProvinceEn = "X",
            SubDistrictTh = "X",
            DistrictTh = "X",
            ProvinceTh = "X"
        };
        var result = await service.UpdateAsync(missing);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var result = await service.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }
}
