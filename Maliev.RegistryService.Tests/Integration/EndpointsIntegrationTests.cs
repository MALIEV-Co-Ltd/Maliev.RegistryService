using System.Net;
using System.Net.Http.Json;
using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Domain.Entities;
using Maliev.RegistryService.Infrastructure.Services;
using Maliev.RegistryService.Tests.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Integration;

[Trait("Category", "Integration")]
public class EndpointsIntegrationTests : IClassFixture<RegistryServiceTestFactory>
{
    private readonly RegistryServiceTestFactory _factory;

    public EndpointsIntegrationTests(RegistryServiceTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Autocomplete_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        // Note: Data is seeded automatically via EF Migrations in the TestFactory

        // Act
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete?query=Bangkok");


        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ThaiLocation>>>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Data!);
    }

    [Fact]
    public async Task Autocomplete_WithPostalCode_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);

        // Act
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete?query=10200");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ThaiLocation>>>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Data!);
        Assert.All(result.Data!, l => Assert.StartsWith("10200", l.PostalCode));
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        var id = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/registry/v1/thai/addresses/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Autocomplete_WithInvalidQuery_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);


        // Act
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete?query=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_ReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert
        var paths = new[] { "/registry/liveness", "/registry/readiness", "/registry/aspire-liveness" };
        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return; // Success
            }
        }

        Assert.Fail("None of the health check endpoints returned OK.");
    }

    [Fact]
    public async Task CompanySearch_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var mockRegistryService = new Mock<IThaiCompanyRegistryService>();
        mockRegistryService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompanyProfile>
            {
                new CompanyProfile("1", "ยังดำเนินกิจการอยู่", "0105552101137",
                    "มาลี ฟาซาด เอ็นจิเนียริ่ง เซอร์วิส",
                    "ประกอบกิจการรับเป็นที่ปรึกษา",
                    "5", null, "บริษัท มาลี ฟาซาด เอ็นจิเนียริ่ง เซอร์วิส จำกัด")
            });

        // Create a modified factory with the mock service
        var modifiedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing IThaiCompanyRegistryService registrations
                var descriptors = services.Where(
                    d => d.ServiceType == typeof(IThaiCompanyRegistryService)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add mock service
                services.AddScoped(_ => mockRegistryService.Object);
            });
        });

        // Create authenticated client from the modified factory
        var token = _factory.CreateTestJwtToken(permissions: [RegistryPermissions.CompaniesRead]);
        var client = modifiedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=มาลีฟ");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<CompanyProfile>>>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Data!);
    }

    [Fact]
    public async Task CompanySearch_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.CompaniesRead]);

        // Act
        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompanySearch_WithoutPermission_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: []);

        // Act
        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompanySearch_WithLimitZero_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.CompaniesRead]);
        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=test&limit=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompanySearch_WithLimitOver100_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.CompaniesRead]);
        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=test&limit=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompanySearch_WhenServiceThrowsInvalidOperationException_ReturnsServiceUnavailable()
    {
        var mockRegistryService = new Mock<IThaiCompanyRegistryService>();
        mockRegistryService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var modifiedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(IThaiCompanyRegistryService)).ToList();
                foreach (var descriptor in descriptors) services.Remove(descriptor);
                services.AddScoped(_ => mockRegistryService.Object);
            });
        });

        var token = _factory.CreateTestJwtToken(permissions: [RegistryPermissions.CompaniesRead]);
        var client = modifiedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=test");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CompanySearch_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var mockRegistryService = new Mock<IThaiCompanyRegistryService>();
        mockRegistryService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var modifiedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(IThaiCompanyRegistryService)).ToList();
                foreach (var descriptor in descriptors) services.Remove(descriptor);
                services.AddScoped(_ => mockRegistryService.Object);
            });
        });

        var token = _factory.CreateTestJwtToken(permissions: [RegistryPermissions.CompaniesRead]);
        var client = modifiedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/registry/v1/thai/companies/search?query=test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task AutocompleteMultiField_WithAllEmptyParams_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete-multi");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AutocompleteMultiField_WithPostalCode_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete-multi?postalCode=10200");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ThaiLocation>>>();
        Assert.True(result!.Success);
    }

    [Fact]
    public async Task AutocompleteMultiField_WithDistrict_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete-multi?district=Bangkok");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ThaiLocation>>>();
        Assert.True(result!.Success);
    }
}
