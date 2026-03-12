using System.Text.Json.Serialization;

namespace Maliev.RegistryService.Application.DTOs;

/// <summary>
/// Response from dataforthai.com company search API.
/// </summary>
/// <param name="Status">API response status (1 = success).</param>
/// <param name="Data">Array of company search results.</param>
/// <param name="Request">Echo of the original request parameters.</param>
public sealed record DataForThaiResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("data")] List<DataForThaiCompanyData> Data,
    [property: JsonPropertyName("req")] DataForThaiRequestEcho? Request
);

/// <summary>
/// Individual company data from dataforthai.com response.
/// </summary>
public sealed record DataForThaiCompanyData(
    [property: JsonPropertyName("status_code")] string StatusCode,
    [property: JsonPropertyName("status_tname")] string StatusNameTh,
    [property: JsonPropertyName("jp_no")] string TaxId,
    [property: JsonPropertyName("jp_tname")] string CompanyNameTh,
    [property: JsonPropertyName("obj_name_keyin")] string BusinessObjectives,
    [property: JsonPropertyName("jp_type_code")] string CompanyTypeCode,
    [property: JsonPropertyName("stock_name")] string? StockName,
    [property: JsonPropertyName("full_tname")] string FullNameTh
);

/// <summary>
/// Echo of the request parameters from dataforthai.com.
/// </summary>
public sealed record DataForThaiRequestEcho(
    [property: JsonPropertyName("searchtext")] string SearchText
);
