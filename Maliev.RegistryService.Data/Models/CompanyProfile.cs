namespace Maliev.RegistryService.Data.Models;

public record CompanyProfile(
    string TaxId,
    string NameTh,
    string NameEn,
    string Status,
    string Source,
    string Address = "",
    string BusinessType = "",
    string RegistrationDate = "",
    string Capital = ""
);
