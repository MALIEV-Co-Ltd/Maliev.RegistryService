namespace Maliev.RegistryService.Application.DTOs;

/// <summary>
/// Represents a company profile retrieved from the Thai business registry.
/// </summary>
/// <param name="StatusCode">Company status code (1=Active, 5=Liquidated, 8=Vacant).</param>
/// <param name="StatusNameTh">Status description in Thai.</param>
/// <param name="TaxId">The 13-digit tax identification number (jp_no).</param>
/// <param name="CompanyNameTh">The company name in Thai (jp_tname).</param>
/// <param name="BusinessObjectives">Business objectives description (obj_name_keyin).</param>
/// <param name="CompanyTypeCode">Type of business entity (3=Partnership, 5=Limited Company).</param>
/// <param name="StockName">Stock name for listed companies (null if not listed).</param>
/// <param name="FullNameTh">Full company name in Thai with prefix (full_tname).</param>
public sealed record CompanyProfile(
    string StatusCode,
    string StatusNameTh,
    string TaxId,
    string CompanyNameTh,
    string BusinessObjectives,
    string CompanyTypeCode,
    string? StockName,
    string FullNameTh
);
