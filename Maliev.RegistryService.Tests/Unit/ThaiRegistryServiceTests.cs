using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Domain.Entities;
using Maliev.RegistryService.Infrastructure.Persistence;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class ThaiRegistryServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<RegistryDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _options = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.RowLimitingOperationWithoutOrderByWarning))
            .Options;

        using var context = new RegistryDbContext(_options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsLocation()
    {
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "12345", SubDistrictEn = "Test", DistrictEn = "Test", ProvinceEn = "Test", SubDistrictTh = "Test", DistrictTh = "Test", ProvinceTh = "Test" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_SavesToDatabase()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var location = new ThaiLocation { PostalCode = "55555", SubDistrictEn = "New", DistrictEn = "New", ProvinceEn = "New", SubDistrictTh = "New", DistrictTh = "New", ProvinceTh = "New" };

        var result = await service.CreateAsync(location);

        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await context.ThaiLocations.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("55555", saved.PostalCode);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingData()
    {
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "10000", SubDistrictEn = "Old", DistrictEn = "Old", ProvinceEn = "Old", SubDistrictTh = "Old", DistrictTh = "Old", ProvinceTh = "Old" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);
        var updated = new ThaiLocation { Id = id, PostalCode = "20000", SubDistrictEn = "New", DistrictEn = "New", ProvinceEn = "New", SubDistrictTh = "New", DistrictTh = "New", ProvinceTh = "New" };

        var success = await service.UpdateAsync(updated);

        Assert.True(success);
        var saved = await context.ThaiLocations.FindAsync(id);
        Assert.Equal("20000", saved!.PostalCode);
        Assert.Equal("New", saved.SubDistrictEn);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFromDatabase()
    {
        var id = Guid.NewGuid();
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = id, PostalCode = "10000", SubDistrictEn = "Old", DistrictEn = "Old", ProvinceEn = "Old", SubDistrictTh = "Old", DistrictTh = "Old", ProvinceTh = "Old" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var success = await service.DeleteAsync(id);

        Assert.True(success);
        var saved = await context.ThaiLocations.FindAsync(id);
        Assert.Null(saved);
    }

    [Fact]
    public async Task AutocompleteAsync_WithEmptyQuery_ReturnsEmpty()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteAsync("", 10);

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

    [Fact]
    public async Task AutocompleteAsync_WithValidQuery_ReturnsMatchingLocations()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10200", SubDistrictTh = "สามเสนใน", DistrictTh = "ป้อมปราบ", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Sam Sen Nai", DistrictEn = "Pom Prap", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteAsync("สีลม", 10);

        Assert.Single(result);
        Assert.Equal("10100", result.First().PostalCode);
    }

    [Fact]
    public async Task AutocompleteAsync_WithLimit_RespectsLimit()
    {
        using var context = new RegistryDbContext(_options);
        for (int i = 0; i < 5; i++)
        {
            context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = $"10{i:000}", SubDistrictTh = $"Test{i}", DistrictTh = "Test", ProvinceTh = "Test", SubDistrictEn = "Test", DistrictEn = "Test", ProvinceEn = "Test" });
        }
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteAsync("Test", 2);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AutocompleteAsync_WithWhitespaceQuery_ReturnsEmpty()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteAsync("   ", 10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithPostalCode_ReturnsMatching()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync("10100", null, null, null, 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithDistrict_ReturnsMatching()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync(null, "สีลม", null, null, 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithCity_ReturnsMatching()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync(null, null, "บางรัก", null, 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithEnglishDistrict_ReturnsSubDistrictMatch()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "11120", SubDistrictTh = "คลองข่อย", DistrictTh = "ปากเกร็ด", ProvinceTh = "นนทบุรี", SubDistrictEn = "Khlong Khoi", DistrictEn = "Pak Kret", ProvinceEn = "Nonthaburi" });
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10110", SubDistrictTh = "คลองเตย", DistrictTh = "คลองเตย", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Khlong Toei", DistrictEn = "Khlong Toei", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync(null, "Khlong Khoi", null, null, 10);

        var location = Assert.Single(result);
        Assert.Equal("11120", location.PostalCode);
        Assert.Equal("Khlong Khoi", location.SubDistrictEn);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithEnglishCity_ReturnsDistrictMatch()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "11120", SubDistrictTh = "คลองข่อย", DistrictTh = "ปากเกร็ด", ProvinceTh = "นนทบุรี", SubDistrictEn = "Khlong Khoi", DistrictEn = "Pak Kret", ProvinceEn = "Nonthaburi" });
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10110", SubDistrictTh = "คลองเตย", DistrictTh = "คลองเตย", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Khlong Toei", DistrictEn = "Khlong Toei", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync(null, null, "Pak Kret", null, 10);

        var location = Assert.Single(result);
        Assert.Equal("11120", location.PostalCode);
        Assert.Equal("Pak Kret", location.DistrictEn);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithProvince_ReturnsMatching()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync(null, null, null, "กรุงเทพ", 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task AutocompleteMultiFieldAsync_WithMultipleFields_ReturnsMatching()
    {
        using var context = new RegistryDbContext(_options);
        context.ThaiLocations.Add(new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100", SubDistrictTh = "สีลม", DistrictTh = "บางรัก", ProvinceTh = "กรุงเทพมหานคร", SubDistrictEn = "Silom", DistrictEn = "Bang Rak", ProvinceEn = "Bangkok" });
        await context.SaveChangesAsync();

        var service = new ThaiRegistryService(context);

        var result = await service.AutocompleteMultiFieldAsync("10100", "สีลม", "บางรัก", "กรุงเทพ", 10);

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyId_GeneratesNewGuid()
    {
        using var context = new RegistryDbContext(_options);
        var service = new ThaiRegistryService(context);
        var location = new ThaiLocation { PostalCode = "55555", SubDistrictEn = "New", DistrictEn = "New", ProvinceEn = "New", SubDistrictTh = "New", DistrictTh = "New", ProvinceTh = "New" };

        var result = await service.CreateAsync(location);

        Assert.NotEqual(Guid.Empty, result.Id);
    }
}
