using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Data.Entities;
using Maliev.RegistryService.Tests.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Maliev.RegistryService.Tests.Integration;

public class LocationsCrudTests : IClassFixture<RegistryServiceTestFactory>
{
    private readonly RegistryServiceTestFactory _factory;

    public LocationsCrudTests(RegistryServiceTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]);
        
        // Use a known ID from the seed data (Bangkok, Phra Nakhon, Phra Borom Maha Ratchawang)
        var id = new Guid("ec9a6fd1-d909-4ad7-b475-ea60adf0e414");

        // Act
        var response = await client.GetAsync($"/registry/v1/thai/addresses/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ThaiLocation>>();
        Assert.True(result!.Success);
        Assert.Equal(id, result.Data!.Id);
    }

    [Fact]
    public async Task Manage_FullCrudCycle_Success()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.RegistryManage, RegistryPermissions.LocationsRead]);
        var newLocation = new ThaiLocation
        {
            Id = Guid.NewGuid(),
            PostalCode = "99999",
            SubDistrictTh = "ทดสอบ",
            DistrictTh = "ทดสอบ",
            ProvinceTh = "ทดสอบ",
            SubDistrictEn = "Test",
            DistrictEn = "Test",
            ProvinceEn = "Test"
        };

        // 1. Create
        var createResponse = await client.PostAsJsonAsync("/registry/v1/thai/addresses", newLocation);
        createResponse.EnsureSuccessStatusCode();
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ThaiLocation>>();
        Assert.Equal(newLocation.Id, createResult!.Data!.Id);

        // 2. Read
        var getResponse = await client.GetAsync($"/registry/v1/thai/addresses/{newLocation.Id}");
        getResponse.EnsureSuccessStatusCode();
        
        // 3. Update
        newLocation.PostalCode = "88888";
        var updateResponse = await client.PutAsJsonAsync($"/registry/v1/thai/addresses/{newLocation.Id}", newLocation);
        updateResponse.EnsureSuccessStatusCode();
        var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ThaiLocation>>();
        Assert.Equal("88888", updateResult!.Data!.PostalCode);

        // 4. Delete
        var deleteResponse = await client.DeleteAsync($"/registry/v1/thai/addresses/{newLocation.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        // 5. Verify Deleted
        var getDeletedResponse = await client.GetAsync($"/registry/v1/thai/addresses/{newLocation.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(permissions: [RegistryPermissions.LocationsRead]); // Missing Manage
        var newLocation = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "11111" };

        // Act
        var response = await client.PostAsJsonAsync("/registry/v1/thai/addresses", newLocation);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
