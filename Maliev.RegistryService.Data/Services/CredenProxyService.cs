using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Maliev.RegistryService.Data.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Data.Services;

/// <summary>
/// Service interface for Creden.co company lookups.
/// </summary>
public interface ICredenProxyService
{
    /// <summary>
    /// Searches for Thai companies via Creden.co API.
    /// </summary>
    /// <param name="searchText">The search query (company name or tax ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching company profiles.</returns>
    Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(string searchText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Proxy service for querying Thai business registry data via Creden.co API.
/// </summary>
public sealed class CredenProxyService : ICredenProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CredenProxyService> _logger;
    private const int CacheExpirationHours = 24;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredenProxyService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for external API calls.</param>
    /// <param name="cache">Distributed cache for storing results.</param>
    /// <param name="logger">Logger instance.</param>
    public CredenProxyService(
        HttpClient httpClient,
        IDistributedCache cache,
        ILogger<CredenProxyService> logger)
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
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Enumerable.Empty<CompanyProfile>();
        }

        var cacheKey = $"creden:search:{searchText}";
        var cachedData = await _cache.GetAsync(cacheKey, cancellationToken);

        if (cachedData != null)
        {
            try
            {
                var json = Encoding.UTF8.GetString(cachedData);
                return JsonSerializer.Deserialize<IEnumerable<CompanyProfile>>(json) ?? Enumerable.Empty<CompanyProfile>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached Creden data for query '{Query}'", searchText);
            }
        }

        try
        {
            var payload = new CredenSearchRequest
            {
                TypeSearch = "prefix",
                Text = searchText,
                Lang = "th"
            };

            var response = await _httpClient.PostAsJsonAsync("sapi/search/get_suggestion", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var credenResponse = await response.Content.ReadFromJsonAsync<CredenResponse>(cancellationToken);

            if (credenResponse?.Success != true || credenResponse.Data?.Result == null)
            {
                _logger.LogWarning("Creden API search returned no results or failed for query '{Query}'", searchText);
                return Enumerable.Empty<CompanyProfile>();
            }

            var profiles = credenResponse.Data.Result.Select(MapToCompanyProfile).ToList();

            // Cache the result for 24 hours
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheExpirationHours)
            };
            var jsonData = JsonSerializer.Serialize(profiles);
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(jsonData), cacheOptions, cancellationToken);

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while searching companies via Creden for query '{Query}'", searchText);
            return Enumerable.Empty<CompanyProfile>();
        }
    }

    private static CompanyProfile MapToCompanyProfile(CredenCompanyResult result)
    {
        return new CompanyProfile(
            StatusCode: "1", // Assume active if returned by search
            StatusNameTh: "ยังดำเนินกิจการอยู่",
            TaxId: result.Id,
            CompanyNameTh: result.CompanyName?.Th ?? string.Empty,
            BusinessObjectives: string.Empty,
            CompanyTypeCode: "0",
            StockName: null,
            FullNameTh: result.CompanyName?.Th ?? string.Empty
        );
    }
}
