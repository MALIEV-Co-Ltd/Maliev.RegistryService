using System.Text.Json.Serialization;

namespace Maliev.RegistryService.Infrastructure.Services;

/// <summary>
/// Request body sent to the Creden.co suggestion search endpoint.
/// </summary>
internal sealed record CredenSearchRequest
{
    /// <summary>Gets or sets the search strategy (e.g. "prefix").</summary>
    [JsonPropertyName("type_search")]
    public string TypeSearch { get; init; } = "prefix";

    /// <summary>Gets or sets the search text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    /// <summary>Gets or sets the result language ("th" or "en").</summary>
    [JsonPropertyName("lang")]
    public string Lang { get; init; } = "th";
}

/// <summary>
/// Top-level response wrapper returned by the Creden.co API.
/// </summary>
internal sealed record CredenResponse
{
    /// <summary>Gets or sets whether the request succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Gets or sets the response data payload.</summary>
    [JsonPropertyName("data")]
    public CredenData? Data { get; init; }
}

/// <summary>
/// Data envelope within a <see cref="CredenResponse"/>.
/// </summary>
internal sealed record CredenData
{
    /// <summary>Gets or sets the list of matching company results.</summary>
    [JsonPropertyName("result")]
    public List<CredenCompanyResult>? Result { get; init; }
}

/// <summary>
/// A single company entry returned by the Creden.co search endpoint.
/// </summary>
internal sealed record CredenCompanyResult
{
    /// <summary>Gets or sets the 13-digit juristic tax ID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets or sets the company name in available languages.</summary>
    [JsonPropertyName("company_name")]
    public CredenCompanyName? CompanyName { get; init; }
}

/// <summary>
/// Company name localisation returned by Creden.co.
/// </summary>
internal sealed record CredenCompanyName
{
    /// <summary>Gets or sets the English company name.</summary>
    [JsonPropertyName("en")]
    public string? En { get; init; }

    /// <summary>Gets or sets the Thai company name.</summary>
    [JsonPropertyName("th")]
    public string? Th { get; init; }
}
