namespace Maliev.RegistryService.Api.Authorization;

/// <summary>
/// Predefined roles for the Registry Service.
/// </summary>
public static class RegistryPredefinedRoles
{
    /// <summary>
    /// Represents a role definition.
    /// </summary>
    /// <param name="RoleId">The unique identifier for the role.</param>
    /// <param name="Description">A description of what the role allows.</param>
    /// <param name="Permissions">The list of permissions associated with the role.</param>
    public record RoleDef(string RoleId, string Description, string[] Permissions);

    /// <summary>
    /// Viewer role with read-only access to all registry data.
    /// </summary>
    public static readonly RoleDef Viewer = new(
        "roles.registry.viewer",
        "Read-only access to all registry data",
        [RegistryPermissions.LocationsRead, RegistryPermissions.CompaniesRead]
    );

    /// <summary>
    /// Admin role with full access to registry service.
    /// </summary>
    public static readonly RoleDef Admin = new(
        "roles.registry.admin",
        "Administrative access to registry service",
        [RegistryPermissions.LocationsRead, RegistryPermissions.CompaniesRead, RegistryPermissions.RegistryManage]
    );

    /// <summary>
    /// Gets all predefined roles.
    /// </summary>
    public static readonly IEnumerable<RoleDef> All = [Viewer, Admin];
}
