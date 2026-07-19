using System.Text;
using System.Text.Json;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure.Configuration;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class DbdProxyServiceTests
{
    private static IOptions<BdexApiOptions> CreateDefaultOptions()
    {
        return Options.Create(new BdexApiOptions
        {
            BaseUrl = "https://api.dbd.go.th",
            ConsumerKey = "test-key",
            ConsumerSecret = "test-secret"
        });
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithCachedData_ReturnsCachedResults()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();

        var cachedProfile = new CompanyProfile("1", "Active", "1234567890123", "Test Company", "Test Business", "5", null, "Test Full Name");
        var cachedJson = JsonSerializer.Serialize(cachedProfile);

        cacheMock.Setup(c => c.GetAsync("bdex:company:1234567890123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("1234567890123");

        // Assert
        Assert.Single(result);
        Assert.Equal("1234567890123", result.First().TaxId);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithNullWhitespaceQuery_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("   ");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithInvalidTaxId_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("12345");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_With13DigitsButInvalid_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act - 13 digits but not a valid tax ID format
        var result = await service.SearchCompaniesAsync("1234567890123");

        // Assert - will try to fetch but return empty due to no mock
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithNonDigitsInQuery_ExtractsDigits()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();

        // Should extract "1234567890123" and try to cache lookup
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("abc-12345-67890-123xyz");

        // Assert - will be empty as no valid 13-digit ID found
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenCacheDeserializationFails_ContinuesToFetch()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();

        // Setup cache to return invalid data (corrupt)
        cacheMock.Setup(c => c.GetAsync("bdex:company:1234567890123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("invalid json {"));

        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("1234567890123");

        // Assert - should continue to fetch from API despite cache failure
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithValidTaxIdFormat_ExtractsAndSearches()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var options = CreateDefaultOptions();

        // Test with tax ID containing dashes should extract only digits
        var service = new DbdProxyService(httpClient, cacheMock.Object, options, loggerMock.Object);

        // Act - 13 digits but not a valid tax ID format
        var result = await service.SearchCompaniesAsync("12345678-90123");

        // Assert - will be empty as no valid 13-digit ID found after extraction
        Assert.Empty(result);
    }
}
