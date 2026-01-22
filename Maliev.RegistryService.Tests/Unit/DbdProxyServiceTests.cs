using Maliev.RegistryService.Data.Models;
using Maliev.RegistryService.Data.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class DbdProxyServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DbdProxyService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly DbdProxyService _service;

    public DbdProxyServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DbdProxyService>>();
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _service = new DbdProxyService(_mockCache.Object, _mockLogger.Object, _httpClient);
    }

    [Fact]
    public async Task LookupAsync_WhenCached_ReturnsCachedData()
    {
        // Arrange
        var query = "test";
        var cachedResults = new List<CompanyProfile> { new("123", "Test", "", "Active", "Source") };
        var cachedJson = JsonSerializer.Serialize(cachedResults);
        
        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        // Act
        var result = await _service.LookupAsync(query, 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("123", result.First().TaxId);
        _mockHandler.Protected().Verify(
            "SendAsync", 
            Times.Never(), 
            ItExpr.IsAny<HttpRequestMessage>(), 
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LookupAsync_WhenNotCached_FetchesAndCaches()
    {
        // Arrange
        var query = "1234567890123";
        var searchJsonResponse = "{\"data\": [{\"jp_no\": \"1234567890123\", \"full_tname\": \"Test Co\", \"status_tname\": \"Active\"}]}";
        var detailHtmlResponse = "<html><body><h3>Test Company EN</h3><table><tr><td>จดทะเบียน</td><td>01/01/2020</td></tr></table></body></html>";

        // Setup search API response
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.ToString().Contains("api/company")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(searchJsonResponse)
            });

        // Setup detail printview response
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.ToString().Contains("printview")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(detailHtmlResponse)
            });

        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.LookupAsync(query, 1);

        // Assert
        Assert.Single(result);
        var profile = result.First();
        Assert.Equal("1234567890123", profile.TaxId);
        Assert.Equal("Test Company EN", profile.NameEn);
        Assert.Equal("01/01/2020", profile.RegistrationDate);

        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            default), Times.Once());
    }

    [Fact]
    public async Task LookupAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Act
        var result = await _service.LookupAsync("", 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task LookupAsync_WhenUpstreamFails_ThrowsException()
    {
        // Arrange
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[]?)null);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.LookupAsync("fail", 10));
    }
}
