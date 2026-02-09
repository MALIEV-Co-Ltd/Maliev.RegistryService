using Maliev.RegistryService.Data.Models;
using Maliev.RegistryService.Data.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class DbdProxyServiceTests
{
    [Fact]
    public async Task SearchCompaniesAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var service = new DbdProxyService(httpClient, cacheMock.Object, loggerMock.Object);

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

        var cachedProfiles = new List<CompanyProfile>
        {
            new CompanyProfile("1", "Active", "1234567890123", "Test Company", "Test Business", "5", null, "Test Full Name")
        };
        var cachedJson = JsonSerializer.Serialize(cachedProfiles);

        cacheMock.Setup(c => c.GetAsync("dbd:search:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        var service = new DbdProxyService(httpClient, cacheMock.Object, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("test");

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
        var service = new DbdProxyService(httpClient, cacheMock.Object, loggerMock.Object);

        // Act
        var result = await service.SearchCompaniesAsync("   ");

        // Assert
        Assert.Empty(result);
    }
}
