using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class RegistryIAMRegistrationServiceTests
{
    [Fact]
    public void GetPermissions_ReturnsAllDefinedPermissions()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<RegistryIAMRegistrationService>>();
        var service = new RegistryIAMRegistrationService(mockConfig.Object, mockLogger.Object);

        // Act
        // We use reflection to access the protected GetPermissions if needed, 
        // but here we can just test the public behavior or use a wrapper.
        // For simplicity, we check if the permissions match the RegistryPermissions class.

        // Since it's protected, we'll use a testable subclass if needed, 
        // but RegistryIAMRegistrationService IS already the subclass.
        var permissions = typeof(RegistryIAMRegistrationService)
            .GetMethod("GetPermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, null) as IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.PermissionRegistration>;

        // Assert
        Assert.NotNull(permissions);
        Assert.Equal(RegistryPermissions.AllWithDescriptions.Count, permissions.Count());
        Assert.Contains(permissions, p => p.PermissionId == RegistryPermissions.LocationsRead);
    }

    [Fact]
    public void GetPredefinedRoles_ReturnsAllDefinedRoles()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<RegistryIAMRegistrationService>>();
        var service = new RegistryIAMRegistrationService(mockConfig.Object, mockLogger.Object);

        // Act
        var roles = typeof(RegistryIAMRegistrationService)
            .GetMethod("GetPredefinedRoles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, null) as IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration>;

        // Assert
        Assert.NotNull(roles);
        Assert.Equal(RegistryPredefinedRoles.All.Count(), roles.Count());
        Assert.Contains(roles, r => r.RoleId == RegistryPredefinedRoles.Admin.RoleId);
    }
}
