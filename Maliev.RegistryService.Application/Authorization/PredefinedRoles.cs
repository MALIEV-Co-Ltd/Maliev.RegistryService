namespace Maliev.RegistryService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Registry Service.
/// </summary>
public static class RegistryPredefinedRoles
{
    public const string Admin = "roles.registry.admin";
    public const string Viewer = "roles.registry.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Registry Administrator with full access",
            new[]
            {
                RegistryPermissions.LocationRead,
                RegistryPermissions.LocationManage,
                RegistryPermissions.CompanyRead,
                RegistryPermissions.CompanyManage,
                RegistryPermissions.SettingsRead,
                RegistryPermissions.SettingsUpdate,
            }
        ),
        (
            Viewer,
            "Registry Viewer with read-only access",
            new[]
            {
                RegistryPermissions.LocationRead,
                RegistryPermissions.CompanyRead,
                RegistryPermissions.SettingsRead,
            }
        ),
    };
}
