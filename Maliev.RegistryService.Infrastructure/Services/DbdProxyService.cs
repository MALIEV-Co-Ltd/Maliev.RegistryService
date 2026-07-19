using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.RegistryService.Infrastructure.Services;

/// <summary>
/// Proxy service for querying Thai business registry data via BDEX API (api.dbd.go.th).
/// Implements OAuth 2.0 authentication with token caching and 24-hour result caching.
/// </summary>
public sealed class DbdProxyService : IDbdProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DbdProxyService> _logger;
    private readonly BdexApiOptions _options;
    private const int CacheExpirationHours = 24;
    private const string TokenCacheKey = "bdex:oauth:token";

    /// <summary>
    /// Initializes a new instance of the <see cref="DbdProxyService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for external API calls.</param>
    /// <param name="cache">Distributed cache for storing results.</param>
    /// <param name="options">BDEX API configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public DbdProxyService(
        HttpClient httpClient,
        IDistributedCache cache,
        IOptions<BdexApiOptions> options,
        ILogger<DbdProxyService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
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

        // Check if the search text looks like a tax ID (13 digits)
        var sanitizedSearch = new string(searchText.Where(char.IsDigit).ToArray());
        if (sanitizedSearch.Length != 13)
        {
            _logger.LogDebug("Query '{SearchText}' is not a 13-digit tax ID — BDEX skipped.", searchText);
            return Enumerable.Empty<CompanyProfile>();
        }

        var cacheKey = $"bdex:company:{sanitizedSearch}";
        var cachedData = await _cache.GetAsync(cacheKey, cancellationToken);

        if (cachedData != null)
        {
            try
            {
                var json = Encoding.UTF8.GetString(cachedData);
                var profile = JsonSerializer.Deserialize<CompanyProfile>(json);
                return profile != null ? [profile] : Enumerable.Empty<CompanyProfile>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached company data for tax ID '{TaxId}'", sanitizedSearch);
            }
        }

        try
        {
            // Get OAuth token (cached)
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to obtain BDEX access token");
                return Enumerable.Empty<CompanyProfile>();
            }

            // Lookup company by juristic ID
            var companyData = await LookupCompanyByIdAsync(sanitizedSearch, accessToken, cancellationToken);
            if (companyData == null)
            {
                return Enumerable.Empty<CompanyProfile>();
            }

            // Map to CompanyProfile
            var profile = MapToCompanyProfile(companyData);

            // Cache the result for 24 hours
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheExpirationHours)
            };
            var jsonData = JsonSerializer.Serialize(profile);
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(jsonData), cacheOptions, cancellationToken);

            return [profile];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while looking up company with tax ID '{TaxId}'", sanitizedSearch);
            return Enumerable.Empty<CompanyProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while looking up company with tax ID '{TaxId}'", sanitizedSearch);
            return Enumerable.Empty<CompanyProfile>();
        }
    }

    /// <summary>
    /// Retrieves an OAuth access token from BDEX API, with caching.
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Check cache first
        var cachedToken = await _cache.GetAsync(TokenCacheKey, cancellationToken);
        if (cachedToken != null)
        {
            try
            {
                return Encoding.UTF8.GetString(cachedToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached BDEX token");
            }
        }

        try
        {
            // Create Basic authentication header
            var credentials = $"{_options.ConsumerKey}:{_options.ConsumerSecret}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}{_options.RequestAccessToken}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
            request.Content = JsonContent.Create(new BdexTokenRequest());

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<BdexResponse<BdexTokenData>>(cancellationToken);

            if (tokenResponse?.Status.IsSuccess != true || tokenResponse.Data == null)
            {
                var errorMessage = GetBdexErrorMessage(tokenResponse?.Status.Code, tokenResponse?.Status.Description);
                _logger.LogError("BDEX token request failed: {StatusCode} - {StatusDescription} ({ErrorMessage})",
                    tokenResponse?.Status.Code, tokenResponse?.Status.Description, errorMessage);
                return null;
            }

            var accessToken = tokenResponse.Data.AccessToken;

            // Cache the token (slightly less than expiration to avoid edge cases)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.TokenCacheDurationSeconds)
            };
            await _cache.SetAsync(TokenCacheKey, Encoding.UTF8.GetBytes(accessToken), cacheOptions, cancellationToken);

            _logger.LogInformation("Successfully obtained and cached BDEX access token (expires in {ExpiresIn}s)",
                tokenResponse.Data.ExpiresIn);

            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain BDEX access token");
            return null;
        }
    }

    /// <summary>
    /// Looks up company information by juristic ID using the BDEX API.
    /// </summary>
    private async Task<BdexCompanyData?> LookupCompanyByIdAsync(
        string juristicId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}{_options.InquiryOJPbyID}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new BdexCompanyLookupRequest
            {
                OrganizationJuristicID = juristicId
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var companyResponse = await response.Content.ReadFromJsonAsync<BdexResponse<BdexCompanyData>>(cancellationToken);

            if (companyResponse?.Status.IsSuccess != true)
            {
                var errorMessage = GetBdexErrorMessage(companyResponse?.Status.Code, companyResponse?.Status.Description);
                _logger.LogWarning("BDEX company lookup failed for ID '{JuristicId}': {StatusCode} - {StatusDescription} ({ErrorMessage})",
                    juristicId, companyResponse?.Status.Code, companyResponse?.Status.Description, errorMessage);
                return null;
            }

            return companyResponse.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup company with juristic ID '{JuristicId}'", juristicId);
            return null;
        }
    }

    /// <summary>
    /// Maps BDEX company data to the internal CompanyProfile model.
    /// </summary>
    private static CompanyProfile MapToCompanyProfile(BdexCompanyData data)
    {
        // Extract business objectives from the array
        var businessObjectives = string.Empty;
        if (data.OrganizationJuristicObjective?.Count > 0)
        {
            // Combine all objective texts in Thai
            businessObjectives = string.Join("; ",
                data.OrganizationJuristicObjective
                    .Where(obj => !string.IsNullOrWhiteSpace(obj.JuristicObjectiveTextTH))
                    .Select(obj => obj.JuristicObjectiveTextTH));
        }

        return new CompanyProfile(
            StatusCode: data.OrganizationJuristicStatus ?? "0",
            StatusNameTh: data.OrganizationJuristicStatus ?? "ไม่ทราบสถานะ",
            TaxId: data.OrganizationJuristicID ?? string.Empty,
            CompanyNameTh: data.OrganizationJuristicNameTH ?? string.Empty,
            BusinessObjectives: businessObjectives,
            CompanyTypeCode: data.OrganizationJuristicType ?? "0",
            StockName: null, // BDEX API doesn't provide stock symbol directly
            FullNameTh: data.OrganizationJuristicNameTH ?? string.Empty
        );
    }

    /// <summary>
    /// Maps BDEX error codes to user-friendly error messages.
    /// </summary>
    private static string GetBdexErrorMessage(string? code, string? description)
    {
        return code switch
        {
            "1000" => "Success",
            "1001" => "Invalid parameter format: The value could not be parsed as a number",
            "1002" => "Search query too short: A minimum of 5 characters is required",
            "1004" => "No data available: No company found with this ID",
            "1005" => "Rate limit exceeded: Transaction amount or number of transactions is over limit",
            "1051" => "Invalid juristic ID format: รูปแบบเลขทะเบียนนิติบุคคลไม่ถูกต้อง",
            "1052" => "Unsupported entity type: Partnership entities are not supported for this service",
            "1101" => "Missing required parameters",
            "1102" => "Invalid parameters entered",
            "1103" => "Empty string input not supported",
            "1104" => "Requested entity record does not exist",
            "1105" => "Unrecognized field name - Please check spelling",
            "1111" => "Data entry duplicated with existing record",
            "2001" => "Insufficient balance",
            "3001" => "Request is being processed",
            "4101" => "Current channel is not supported",
            "8101" => "Invalid response from downstream service",
            "8102" => "Payment API error",
            "8888" => "Rate limit exceeded: Frequent API calls are exceeding the rate limit",
            "8889" => "Account suspended: Account has been inactive for more than 180 days",
            "8901" => "Database error",
            "9100" => "Missing required authorization credentials or headers",
            "9300" => "Invalid or expired temporary token",
            "9500" => "Invalid authorization credentials",
            "9503" => "Invalid access rights: Insufficient permissions",
            "9700" => "Generic server side error",
            "9900" => "Service error: Server is currently unavailable or threat has been detected",
            "9901" => "System maintenance in progress",
            _ => description ?? "Unknown error occurred"
        };
    }
}
