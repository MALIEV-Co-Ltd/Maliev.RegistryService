using Maliev.RegistryService.Application.DTOs;

namespace Maliev.RegistryService.Application.Interfaces;

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
