using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Infrastructure.Services;

/// <summary>
/// Orchestrator that implements <see cref="IThaiCompanyRegistryService"/> by trying
/// Creden.co first (open, no API key required) and falling back to the government
/// BDEX API (api.dbd.go.th) for 13-digit tax-ID lookups.
/// </summary>
internal sealed class ThaiCompanyRegistryService : IThaiCompanyRegistryService
{
    private readonly ICredenProxyService _creden;
    private readonly IDbdProxyService _bdex;
    private readonly ILogger<ThaiCompanyRegistryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThaiCompanyRegistryService"/> class.
    /// </summary>
    /// <param name="creden">Primary Creden.co lookup service.</param>
    /// <param name="bdex">Fallback BDEX (api.dbd.go.th) lookup service.</param>
    /// <param name="logger">Logger instance.</param>
    public ThaiCompanyRegistryService(
        ICredenProxyService creden,
        IDbdProxyService bdex,
        ILogger<ThaiCompanyRegistryService> logger)
    {
        _creden = creden;
        _bdex = bdex;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Enumerable.Empty<CompanyProfile>();

        // 1. Try Creden (primary — supports both name search and tax-ID lookup)
        try
        {
            var credenResults = (await _creden.SearchCompaniesAsync(searchText, cancellationToken)).ToList();
            if (credenResults.Count > 0)
            {
                _logger.LogDebug("Creden returned {Count} result(s) for query '{Query}'", credenResults.Count, searchText);
                return credenResults;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creden lookup failed for query '{Query}', falling back to BDEX", searchText);
        }

        // 2. Fall back to BDEX (only supports 13-digit tax-ID; name searches will return empty)
        _logger.LogDebug("Creden returned no results for '{Query}', attempting BDEX fallback", searchText);
        return await _bdex.SearchCompaniesAsync(searchText, cancellationToken);
    }
}
