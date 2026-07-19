namespace Maliev.RegistryService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Registry Service.
/// </summary>
public static class RegistryPermissions
{
    public const string LocationRead = "registry.locations.read";
    public const string LocationManage = "registry.locations.manage";

    public const string CompanyRead = "registry.companies.read";
    public const string CompanyManage = "registry.companies.manage";

    public const string SettingsRead = "registry.settings.read";
    public const string SettingsUpdate = "registry.settings.update";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { LocationRead, "Read registry locations" },
        { LocationManage, "Manage registry locations" },
        { CompanyRead, "Read registry companies" },
        { CompanyManage, "Manage registry companies" },
        { SettingsRead, "Read registry settings" },
        { SettingsUpdate, "Update registry settings" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
