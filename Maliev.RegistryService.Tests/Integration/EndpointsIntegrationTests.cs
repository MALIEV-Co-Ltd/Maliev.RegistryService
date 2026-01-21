using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Data.Entities;
using Maliev.RegistryService.Data.Models;
using Maliev.RegistryService.Data.Services;
using Maliev.RegistryService.Tests.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Maliev.RegistryService.Tests.Integration;

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
        var client = _factory.CreateAuthenticatedClient(permissions: ["Registry.Read"]);
        using (var context = _factory.CreateDbContext())
        {
            await _factory.CleanDatabaseAsync();
            context.ThaiLocations.Add(new ThaiLocation 
            { 
                PostalCode = "10110", 
                ProvinceEn = "Bangkok", 
                DistrictEn = "Khlong Toei", 
                SubDistrictEn = "Khlong Toei",
                ProvinceTh = "กรุงเทพ",
                DistrictTh = "คลองเตย",
                SubDistrictTh = "คลองเตย"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/registry/v1/thai/addresses/autocomplete?query=Bangkok");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ThaiLocation>>>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Data!);
    }

    [Fact]
    public async Task CompanyLookup_WithValidTaxId_ReturnsSuccess()
    {
        // Arrange
        var query = "1234567890123";
        var mockResults = new List<CompanyProfile> 
        { 
            new CompanyProfile(query, "Test Company", "Test Company EN", "Active", "https://www.dataforthai.com") 
        };

        var mockDbdService = new Mock<IDbdProxyService>();
        mockDbdService.Setup(s => s.LookupAsync(query, It.IsAny<int>()))
            .ReturnsAsync(mockResults);

        var factory = _factory.WithWebHostBuilder(builder => 
        {
            builder.ConfigureTestServices(services => 
            {
                services.AddScoped(_ => mockDbdService.Object);
            });
        });
        
        var token = _factory.CreateTestJwtToken(permissions: ["Registry.Read"]);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.GetAsync($"/registry/v1/thai/companies/lookup?query={query}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<CompanyProfile>>>();
        Assert.True(result!.Success);
        Assert.NotEmpty(result.Data!);
        Assert.Equal("https://www.dataforthai.com", result.Data!.First().Source);
    }

    [Fact]
    public async Task CompanyLookup_WhenCached_ReturnsSuccess()
    {
        // Arrange
        var query = "9999999999999";
        var mockResults = new List<CompanyProfile> 
        { 
            new CompanyProfile(query, "Cached Company", "Cached Company EN", "Active", "https://www.dataforthai.com") 
        };

        var mockDbdService = new Mock<IDbdProxyService>();
        mockDbdService.Setup(s => s.LookupAsync(query, It.IsAny<int>()))
            .ReturnsAsync(mockResults);

        var factory = _factory.WithWebHostBuilder(builder => 
        {
            builder.ConfigureTestServices(services => 
            {
                services.AddScoped(_ => mockDbdService.Object);
            });
        });

        var token = _factory.CreateTestJwtToken(permissions: ["Registry.Read"]);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act - Call twice to simulate usage
        await client.GetAsync($"/registry/v1/thai/companies/lookup?query={query}");
        var response = await client.GetAsync($"/registry/v1/thai/companies/lookup?query={query}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<CompanyProfile>>>();
        Assert.True(result!.Success);
    }

    [Fact]
    public async Task Autocomplete_WithInvalidQuery_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: ["Registry.Read"]);

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
}
