using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Domain.Entities;

namespace Maliev.RegistryService.Application.Interfaces;

/// <summary>
/// Provides Thai address registry lookup and management operations.
/// </summary>
public interface IThaiRegistryService
{
    /// <summary>
    /// Lists Thai locations using optional query filtering and stable pagination.
    /// </summary>
    Task<PagedResponse<ThaiLocation>> ListAsync(string? query, int pageNumber, int pageSize);

    /// <summary>
    /// Autocompletes Thai locations from a free-text query.
    /// </summary>
    Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit);

    /// <summary>
    /// Autocompletes Thai locations from individual address fields.
    /// </summary>
    Task<IEnumerable<ThaiLocation>> AutocompleteMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3);

    /// <summary>
    /// Gets a Thai location by identifier.
    /// </summary>
    Task<ThaiLocation?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a Thai location.
    /// </summary>
    Task<ThaiLocation> CreateAsync(ThaiLocation location);

    /// <summary>
    /// Updates a Thai location.
    /// </summary>
    Task<bool> UpdateAsync(ThaiLocation location);

    /// <summary>
    /// Deletes a Thai location.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
