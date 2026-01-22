using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Data.Entities;

public class ThaiLocation
{
    public Guid Id { get; set; }

    public string PostalCode { get; set; } = string.Empty;
    public string SubDistrictTh { get; set; } = string.Empty;
    public string DistrictTh { get; set; } = string.Empty;
    public string ProvinceTh { get; set; } = string.Empty;
    public string SubDistrictEn { get; set; } = string.Empty;
    public string DistrictEn { get; set; } = string.Empty;
    public string ProvinceEn { get; set; } = string.Empty;
}
