using System.Text;
using System.Text.Json;
using Maliev.RegistryService.Data.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Data.Services;

/// <summary>
/// Service interface for DBD (Department of Business Development) company lookups.
/// </summary>
public interface IDbdProxyService
{
    /// <summary>
    /// Searches for Thai companies by name or tax ID.
    /// </summary>
    /// <param name="searchText">The search query (company name or tax ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching company profiles.</returns>
    Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(string searchText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Proxy service for querying Thai business registry data via dataforthai.com API.
/// Implements 24-hour Redis caching as per specification.
/// </summary>
public sealed class DbdProxyService : IDbdProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DbdProxyService> _logger;
    private const string BaseUrl = "https://www.dataforthai.com/api/company";
    private const int CacheExpirationHours = 24;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbdProxyService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for external API calls.</param>
    /// <param name="cache">Distributed cache for storing results.</param>
    /// <param name="logger">Logger instance.</param>
    public DbdProxyService(
        HttpClient httpClient,
        IDistributedCache cache,
        ILogger<DbdProxyService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Company search requested for '{SearchText}', but external DBD calls are currently disabled.", searchText);
        return await Task.FromResult(Enumerable.Empty<CompanyProfile>());
    }
}
