namespace Maliev.RegistryService.Api.Authorization;

using Maliev.RegistryService.Api;

/// <summary>
/// Defines all permissions used within the Registry Service.
/// </summary>
public static class RegistryPermissions
{
    /// <summary>
    /// Permission to read and search Thai location data.
    /// </summary>
    public const string LocationsRead = "registry.locations.read";

    /// <summary>
    /// Permission to perform DBD company registry lookups.
    /// </summary>
    public const string CompaniesRead = "registry.companies.read";

    /// <summary>
    /// Permission to manage (create, update, delete) registry data.
    /// </summary>
    public const string RegistryManage = "registry.manage";

    /// <summary>
    /// Gets all permissions with their descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { LocationsRead, "Allows reading and searching Thai location data" },
        { CompaniesRead, "Allows performing DBD company registry lookups" },
        { RegistryManage, "Allows managing (creating, updating, deleting) registry data" }
    };
}
