using Maliev.RegistryService.Infrastructure.Configuration;
using Maliev.RegistryService.Infrastructure.Services;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

/// <summary>
/// Tests for <see cref="DbdProxyService"/> HTTP flow (token + company lookup).
/// </summary>
public class DbdProxyServiceHttpTests
{
    private static IOptions<BdexApiOptions> CreateDefaultOptions() =>
        Options.Create(new BdexApiOptions
        {
            BaseUrl = "https://api.dbd.go.th",
            ConsumerKey = "test-key",
            ConsumerSecret = "test-secret",
            RequestAccessToken = "/auth/oauth/v2/token",
            InquiryOJPbyID = "/text/JuristicPerson/v1/InquiryOJPbyID",
            TokenCacheDurationSeconds = 1700
        });

    private static HttpClient CreateHttpClient(params (string urlPart, string responseJson)[] responses)
    {
        var callCount = 0;
        var responseList = responses.ToList();

        var handler = new FakeHttpMessageHandler(req =>
        {
            var idx = callCount++;
            if (idx < responseList.Count)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseList[idx].responseJson, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        return new HttpClient(handler) { BaseAddress = new Uri("https://api.dbd.go.th") };
    }

    private static HttpClient CreateFailingHttpClient(HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode));
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.dbd.go.th") };
    }

    private static (DbdProxyService service, Mock<IDistributedCache> cacheMock) CreateService(HttpClient httpClient)
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var service = new DbdProxyService(httpClient, cacheMock.Object, CreateDefaultOptions(), loggerMock.Object);
        return (service, cacheMock);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithNon13DigitNumber_ReturnsEmpty()
    {
        var (service, _) = CreateService(new HttpClient());
        var result = await service.SearchCompaniesAsync("1234567890");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithValid13DigitTaxId_FetchesTokenAndCompany_ReturnsProfile()
    {
        const string tokenJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {""accessToken"": ""test-token"", ""tokenType"": ""Bearer"", ""expiresIn"": ""1800"", ""expiresAt"": ""2026-01-01T00:00:00""}
        }";

        const string companyJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {
                ""OrganizationJuristicID"": ""1234567890123"",
                ""OrganizationJuristicNameTH"": ""บริษัท ทดสอบ จำกัด"",
                ""OrganizationJuristicStatus"": ""ยังดำเนินกิจการอยู่"",
                ""OrganizationJuristicType"": ""5"",
                ""OrganizationJuristicObjective"": [
                    {""JuristicObjectiveTextTH"": ""ประกอบกิจการรับเป็นที่ปรึกษา""}
                ]
            }
        }";

        var httpClient = CreateHttpClient(
            ("/auth/oauth/v2/token", tokenJson),
            ("/text/JuristicPerson/v1/InquiryOJPbyID", companyJson));

        var (service, cacheMock) = CreateService(httpClient);

        var result = await service.SearchCompaniesAsync("1234567890123");

        var profile = Assert.Single(result);
        Assert.Equal("1234567890123", profile.TaxId);
        Assert.Equal("บริษัท ทดสอบ จำกัด", profile.CompanyNameTh);
        cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("1234567890123")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenTokenRequestFails_ReturnsEmpty()
    {
        var httpClient = CreateFailingHttpClient(HttpStatusCode.Unauthorized);
        var (service, _) = CreateService(httpClient);

        var result = await service.SearchCompaniesAsync("1234567890123");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenTokenResponseIndicatesFailure_ReturnsEmpty()
    {
        const string failedTokenJson = @"{
            ""status"": {""code"": ""9500"", ""description"": ""Invalid authorization credentials""},
            ""data"": null
        }";

        var httpClient = CreateHttpClient(("/auth/oauth/v2/token", failedTokenJson));
        var (service, _) = CreateService(httpClient);

        var result = await service.SearchCompaniesAsync("1234567890123");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenCompanyLookupFails_ReturnsEmpty()
    {
        const string tokenJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {""accessToken"": ""test-token"", ""tokenType"": ""Bearer"", ""expiresIn"": ""1800"", ""expiresAt"": ""2026-01-01""}
        }";

        const string noCompanyJson = @"{
            ""status"": {""code"": ""1004"", ""description"": ""No data available""},
            ""data"": null
        }";

        var httpClient = CreateHttpClient(
            ("/auth/oauth/v2/token", tokenJson),
            ("/text/JuristicPerson/v1/InquiryOJPbyID", noCompanyJson));

        var (service, _) = CreateService(httpClient);

        var result = await service.SearchCompaniesAsync("1234567890123");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenTokenIsCached_SkipsTokenFetch()
    {
        const string cachedToken = "cached-access-token";
        const string companyJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {
                ""OrganizationJuristicID"": ""9876543210123"",
                ""OrganizationJuristicNameTH"": ""บริษัท ใหม่ จำกัด"",
                ""OrganizationJuristicStatus"": ""ยังดำเนินกิจการอยู่"",
                ""OrganizationJuristicType"": ""5""
            }
        }";

        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync("bdex:oauth:token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedToken));
        cacheMock.Setup(c => c.GetAsync(It.Is<string>(k => k != "bdex:oauth:token"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(companyJson, Encoding.UTF8, "application/json")
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.dbd.go.th") };

        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var service = new DbdProxyService(httpClient, cacheMock.Object, CreateDefaultOptions(), loggerMock.Object);

        var result = await service.SearchCompaniesAsync("9876543210123");

        Assert.Single(result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithCorruptedCacheData_DeserializesGracefully()
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.Is<string>(k => k.Contains("1234567890125")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("invalid json {{{"));
        cacheMock.Setup(c => c.GetAsync("bdex:oauth:token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        const string tokenJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {""accessToken"": ""test-token"", ""tokenType"": ""Bearer"", ""expiresIn"": ""1800"", ""expiresAt"": ""2026-01-01""}
        }";

        const string companyJson = @"{
            ""status"": {""code"": ""1000"", ""description"": ""Success""},
            ""data"": {
                ""OrganizationJuristicID"": ""1234567890125"",
                ""OrganizationJuristicNameTH"": ""บริษัท ทดสอบ 2 จำกัด"",
                ""OrganizationJuristicStatus"": ""ยังดำเนินกิจการอยู่"",
                ""OrganizationJuristicType"": ""5""
            }
        }";

        var responses = new Queue<string>(new[] { tokenJson, companyJson });
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responses.Dequeue(), Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.dbd.go.th") };
        var loggerMock = new Mock<ILogger<DbdProxyService>>();
        var service = new DbdProxyService(httpClient, cacheMock.Object, CreateDefaultOptions(), loggerMock.Object);

        var result = await service.SearchCompaniesAsync("1234567890125");
        Assert.Single(result);
    }
}

/// <summary>
/// Tests for the <see cref="ThaiCompanyRegistryService"/> orchestrator (Creden-first, BDEX-fallback).
/// </summary>
public class ThaiCompanyRegistryServiceTests
{
    private static Mock<IDbdProxyService> BdexReturning(IEnumerable<CompanyProfile> profiles)
    {
        var mock = new Mock<IDbdProxyService>();
        mock.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);
        return mock;
    }

    private static Mock<IDbdProxyService> BdexEmpty() => BdexReturning([]);

    [Fact]
    public async Task SearchCompaniesAsync_WhenCredenReturnsResults_ReturnsThem_WithoutCallingBdex()
    {
        // Arrange
        var credenMock = new Mock<ICredenProxyService>();
        var expected = new[] { new CompanyProfile("1", "Active", "1234567890123", "Creden Co.", "", "5", null, "Creden Co.") };
        credenMock.Setup(s => s.SearchCompaniesAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var bdexMock = BdexEmpty();
        var svc = new ThaiCompanyRegistryService(credenMock.Object, bdexMock.Object,
            new Mock<ILogger<ThaiCompanyRegistryService>>().Object);

        // Act
        var result = await svc.SearchCompaniesAsync("test");

        // Assert
        Assert.Equal(expected, result);
        bdexMock.Verify(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenCredenReturnsEmpty_FallsBackToBdex()
    {
        // Arrange
        var credenMock = new Mock<ICredenProxyService>();
        credenMock.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var bdexProfile = new CompanyProfile("1", "Active", "1234567890123", "BDEX Co.", "", "5", null, "BDEX Co.");
        var bdexMock = BdexReturning([bdexProfile]);

        var svc = new ThaiCompanyRegistryService(credenMock.Object, bdexMock.Object,
            new Mock<ILogger<ThaiCompanyRegistryService>>().Object);

        // Act
        var result = (await svc.SearchCompaniesAsync("1234567890123")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("BDEX Co.", result[0].CompanyNameTh);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WhenCredenThrows_FallsBackToBdex()
    {
        // Arrange
        var credenMock = new Mock<ICredenProxyService>();
        credenMock.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Creden unavailable"));

        var bdexProfile = new CompanyProfile("1", "Active", "1234567890123", "BDEX Co.", "", "5", null, "BDEX Co.");
        var bdexMock = BdexReturning([bdexProfile]);

        var svc = new ThaiCompanyRegistryService(credenMock.Object, bdexMock.Object,
            new Mock<ILogger<ThaiCompanyRegistryService>>().Object);

        // Act
        var result = (await svc.SearchCompaniesAsync("1234567890123")).ToList();

        // Assert — should not throw, should return BDEX result
        Assert.Single(result);
        Assert.Equal("BDEX Co.", result[0].CompanyNameTh);
    }

    [Fact]
    public async Task SearchCompaniesAsync_WithEmptyQuery_ReturnsEmpty_WithoutCallingEitherProvider()
    {
        var credenMock = new Mock<ICredenProxyService>();
        var bdexMock = new Mock<IDbdProxyService>();
        var svc = new ThaiCompanyRegistryService(credenMock.Object, bdexMock.Object,
            new Mock<ILogger<ThaiCompanyRegistryService>>().Object);

        var result = await svc.SearchCompaniesAsync("   ");

        Assert.Empty(result);
        credenMock.Verify(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        bdexMock.Verify(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

/// <summary>
/// Simple fake HTTP handler for unit testing.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="handler">The delegate to invoke for each request.</param>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
