using System.Net;
using Maliev.RegistryService.Data.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.RegistryService.Tests.Services;

public class DbdProxyServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<DbdProxyService>> _loggerMock;

    public DbdProxyServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<DbdProxyService>>();
    }

    [Fact]
    public async Task LookupAsync_RealCall_ShouldReturnResults()
    {
        // Arrange
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer
        };
        var httpClient = new HttpClient(handler);
        
        var service = new DbdProxyService(
            _cacheMock.Object,
            _loggerMock.Object,
            httpClient);

        // Act
        var results = await service.LookupAsync("maliev", 10);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        var maliev = results.FirstOrDefault(r => r.NameTh.Contains("มาลีฟ"));
        Assert.NotNull(maliev);
        Assert.Equal("0125561001573", maliev.TaxId);
        
        // New assertions for expanded data
        Assert.False(string.IsNullOrWhiteSpace(maliev.Address), "Address should not be empty");
        Assert.Contains("นนทบุรี", maliev.Address);
        Assert.False(string.IsNullOrWhiteSpace(maliev.Capital), "Capital should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(maliev.RegistrationDate), "RegistrationDate should not be empty");
    }
}
