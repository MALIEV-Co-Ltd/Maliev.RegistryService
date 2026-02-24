using System.Text.Json.Serialization;

namespace Maliev.RegistryService.Data.Models;

/// <summary>
/// Request body for Creden.co company search.
/// </summary>
internal sealed record CredenSearchRequest
{
    [JsonPropertyName("type_search")]
    public string TypeSearch { get; init; } = "prefix";

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; init; } = "th";
}

/// <summary>
/// Response wrapper for Creden.co API.
/// </summary>
internal sealed record CredenResponse
{
    [JsonPropertyName("data")]
    public CredenData? Data { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

/// <summary>
/// Data container for Creden.co response.
/// </summary>
internal sealed record CredenData
{
    [JsonPropertyName("result")]
    public List<CredenCompanyResult>? Result { get; init; }
}

/// <summary>
/// Company result from Creden.co search.
/// </summary>
internal sealed record CredenCompanyResult
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("company_name")]
    public CredenCompanyName? CompanyName { get; init; }
}

/// <summary>
/// Company name in multiple languages from Creden.co.
/// </summary>
internal sealed record CredenCompanyName
{
    [JsonPropertyName("en")]
    public string? En { get; init; }

    [JsonPropertyName("th")]
    public string? Th { get; init; }
}
