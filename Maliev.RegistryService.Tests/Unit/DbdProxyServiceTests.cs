using Maliev.RegistryService.Infrastructure.Services;
using Maliev.RegistryService.Infrastructure.Configuration;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;
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
}
