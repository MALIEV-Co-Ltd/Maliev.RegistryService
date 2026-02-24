namespace Maliev.RegistryService.Data.Configuration;

/// <summary>
/// Configuration options for the Creden.co API.
/// </summary>
public sealed class CredenApiOptions
{
    /// <summary>
    /// The section name in the configuration file.
    /// </summary>
    public const string SectionName = "Creden";

    /// <summary>
    /// Gets or sets the base URL for the Creden API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://data.creden.co";
}
