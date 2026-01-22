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
    /// Permission to create new registry data.
    /// </summary>
    public const string LocationsCreate = "registry.locations.create";

    /// <summary>
    /// Permission to update existing registry data.
    /// </summary>
    public const string LocationsUpdate = "registry.locations.update";

    /// <summary>
    /// Permission to delete registry data.
    /// </summary>
    public const string LocationsDelete = "registry.locations.delete";

    /// <summary>
    /// Gets all permissions with their descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { LocationsRead, "Allows reading and searching Thai location data" },
        { CompaniesRead, "Allows performing DBD company registry lookups" },
        { LocationsCreate, "Allows creating new Thai location data" },
        { LocationsUpdate, "Allows updating existing Thai location data" },
        { LocationsDelete, "Allows deleting Thai location data" }
    };
}
