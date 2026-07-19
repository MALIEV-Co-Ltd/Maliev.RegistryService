using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Maliev.RegistryService.Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Infrastructure.Services;

/// <summary>
/// Internal interface for Creden.co company lookups.
/// Consumed only by <see cref="ThaiCompanyRegistryService"/>; not exposed to Application layer.
/// </summary>
internal interface ICredenProxyService
{
    /// <summary>
    /// Searches for Thai companies via the Creden.co suggestion API.
    /// </summary>
    /// <param name="searchText">Company name fragment or tax ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching profiles, or an empty collection on failure.</returns>
    Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(
        string searchText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Proxy service for querying Thai business registry data via <c>https://data.creden.co</c>.
/// No API key is required. Results are cached in Redis for 24 hours.
/// </summary>
internal sealed class CredenProxyService : ICredenProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CredenProxyService> _logger;
    private const int CacheExpirationHours = 24;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredenProxyService"/> class.
    /// </summary>
    /// <param name="httpClient">Typed HTTP client targeting data.creden.co.</param>
    /// <param name="cache">Distributed Redis cache.</param>
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
            return Enumerable.Empty<CompanyProfile>();

        var cacheKey = $"creden:search:{searchText.Trim().ToLowerInvariant()}";
        var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);

        if (cachedBytes != null)
        {
            try
            {
                var cached = JsonSerializer.Deserialize<List<CompanyProfile>>(
                    Encoding.UTF8.GetString(cachedBytes));
                if (cached != null)
                    return cached;
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
                Text = searchText.Trim(),
                Lang = "th"
            };

            var response = await _httpClient.PostAsJsonAsync("sapi/search/get_suggestion", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var credenResponse = await response.Content.ReadFromJsonAsync<CredenResponse>(cancellationToken);

            if (credenResponse?.Success != true || credenResponse.Data?.Result == null)
            {
                _logger.LogDebug("Creden returned no results for query '{Query}'", searchText);
                return Enumerable.Empty<CompanyProfile>();
            }

            var profiles = credenResponse.Data.Result
                .Select(MapToCompanyProfile)
                .ToList();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheExpirationHours)
            };
            await _cache.SetAsync(
                cacheKey,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(profiles)),
                cacheOptions,
                cancellationToken);

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creden lookup failed for query '{Query}'", searchText);
            return Enumerable.Empty<CompanyProfile>();
        }
    }

    private static CompanyProfile MapToCompanyProfile(CredenCompanyResult result) =>
        new(
            StatusCode: "1",
            StatusNameTh: "ยังดำเนินกิจการอยู่",
            TaxId: result.Id,
            CompanyNameTh: result.CompanyName?.Th ?? string.Empty,
            BusinessObjectives: string.Empty,
            CompanyTypeCode: "0",
            StockName: null,
            FullNameTh: result.CompanyName?.Th ?? string.Empty
        );
}
