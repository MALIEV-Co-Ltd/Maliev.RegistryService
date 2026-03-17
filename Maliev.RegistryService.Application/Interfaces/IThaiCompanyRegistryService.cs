using Maliev.RegistryService.Application.DTOs;

namespace Maliev.RegistryService.Application.Interfaces;

/// <summary>
/// Provider-agnostic interface for Thai company registry lookups.
/// The implementation tries Creden.co first and falls back to the BDEX API (api.dbd.go.th).
/// </summary>
public interface IThaiCompanyRegistryService
{
    /// <summary>
    /// Searches for Thai companies by name or 13-digit tax ID.
    /// </summary>
    /// <param name="searchText">Company name fragment or 13-digit juristic tax ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching company profiles, or an empty collection if none found.</returns>
    Task<IEnumerable<CompanyProfile>> SearchCompaniesAsync(
        string searchText,
        CancellationToken cancellationToken = default);
}
