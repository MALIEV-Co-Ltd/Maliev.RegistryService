using Maliev.RegistryService.Domain.Entities;

namespace Maliev.RegistryService.Application.Interfaces;

public interface IThaiRegistryService
{
    Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit);
    Task<IEnumerable<ThaiLocation>> AutocompleteMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3);
    Task<ThaiLocation?> GetByIdAsync(Guid id);
    Task<ThaiLocation> CreateAsync(ThaiLocation location);
    Task<bool> UpdateAsync(ThaiLocation location);
    Task<bool> DeleteAsync(Guid id);
}
