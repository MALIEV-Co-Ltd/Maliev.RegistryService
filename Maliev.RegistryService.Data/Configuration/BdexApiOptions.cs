namespace Maliev.RegistryService.Data.Configuration;

/// <summary>
/// Configuration options for BDEX (Business Data Exchange) API integration.
/// </summary>
public sealed class BdexApiOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json or user secrets.
    /// </summary>
    public const string SectionName = "BDEX";

    /// <summary>
    /// The base URL for the BDEX API (default: https://api.dbd.go.th).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.dbd.go.th";

    /// <summary>
    /// OAuth Consumer Key for authentication.
    /// </summary>
    public string ConsumerKey { get; set; } = string.Empty;

    /// <summary>
    /// OAuth Consumer Secret for authentication.
    /// </summary>
    public string ConsumerSecret { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for requesting OAuth access token (default: /auth/oauth/v2/token).
    /// </summary>
    public string RequestAccessToken { get; set; } = "/auth/oauth/v2/token";

    /// <summary>
    /// Endpoint for company inquiry by juristic ID (default: /text/JuristicPerson/v1/InquiryOJPbyID).
    /// </summary>
    public string InquiryOJPbyID { get; set; } = "/text/JuristicPerson/v1/InquiryOJPbyID";

    /// <summary>
    /// Token cache duration in seconds (default: 1700 seconds, slightly less than the 1800s expiration).
    /// </summary>
    public int TokenCacheDurationSeconds { get; set; } = 1700;
}
