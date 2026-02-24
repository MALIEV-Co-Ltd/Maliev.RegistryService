using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Data.Entities;

/// <summary>
/// Represents a location in Thailand (Sub-district, District, Province, Postal Code).
/// </summary>
public class ThaiLocation
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the 5-digit postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub-district name in Thai.</summary>
    public string SubDistrictTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the district name in Thai.</summary>
    public string DistrictTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the province name in Thai.</summary>
    public string ProvinceTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub-district name in English.</summary>
    public string SubDistrictEn { get; set; } = string.Empty;
    /// <summary>Gets or sets the district name in English.</summary>
    public string DistrictEn { get; set; } = string.Empty;
    /// <summary>Gets or sets the province name in English.</summary>
    public string ProvinceEn { get; set; } = string.Empty;
}

