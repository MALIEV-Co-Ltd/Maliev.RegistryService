using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Data.Services;

/// <summary>
/// Service for interacting with the Thailand location registry.
/// </summary>
public interface IThaiRegistryService
{
    /// <summary>
    /// Performs a generic autocomplete search across all location fields.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>A collection of matching locations.</returns>
    Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit);

    /// <summary>
    /// Performs a multi-field search for precise matching.
    /// </summary>
    /// <param name="postalCode">Optional postal code.</param>
    /// <param name="district">Optional district.</param>
    /// <param name="city">Optional city.</param>
    /// <param name="province">Optional province.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>A collection of matching locations.</returns>
    Task<IEnumerable<ThaiLocation>> AutocompleteMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3);

    /// <summary>
    /// Gets a location by its unique identifier.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <returns>The location if found, otherwise null.</returns>
    Task<ThaiLocation?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new location record.
    /// </summary>
    /// <param name="location">The location data.</param>
    /// <returns>The created location.</returns>
    Task<ThaiLocation> CreateAsync(ThaiLocation location);

    /// <summary>
    /// Updates an existing location record.
    /// </summary>
    /// <param name="location">The updated location data.</param>
    /// <returns>True if the update was successful, otherwise false.</returns>
    Task<bool> UpdateAsync(ThaiLocation location);

    /// <summary>
    /// Deletes a location record.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <returns>True if the deletion was successful, otherwise false.</returns>
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// Implementation of the Thai registry service.
/// </summary>
public class ThaiRegistryService : IThaiRegistryService
{
    private readonly RegistryDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThaiRegistryService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ThaiRegistryService(RegistryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ThaiLocation?> GetByIdAsync(Guid id)
    {
        return await _context.ThaiLocations.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<ThaiLocation> CreateAsync(ThaiLocation location)
    {
        if (location.Id == Guid.Empty)
        {
            location.Id = Guid.NewGuid();
        }

        _context.ThaiLocations.Add(location);
        await _context.SaveChangesAsync();
        return location;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ThaiLocation location)
    {
        var existing = await _context.ThaiLocations.FindAsync(location.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(location);
        await _context.SaveChangesAsync();

        await _context.Entry(existing).ReloadAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var location = await _context.ThaiLocations.FindAsync(id);
        if (location == null) return false;

        _context.ThaiLocations.Remove(location);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit)

    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<ThaiLocation>();

        if (query.All(char.IsDigit))
        {
            return await _context.ThaiLocations
                .Where(l => l.PostalCode.StartsWith(query))
                .OrderBy(l => l.PostalCode)
                .Take(limit)
                .ToListAsync();
        }

        var results = await _context.ThaiLocations
            .Where(l =>
                EF.Functions.ILike(l.SubDistrictEn, $"%{query}%") ||
                EF.Functions.ILike(l.DistrictEn, $"%{query}%") ||
                EF.Functions.ILike(l.ProvinceEn, $"%{query}%") ||
                EF.Functions.ILike(l.SubDistrictTh, $"%{query}%") ||
                EF.Functions.ILike(l.DistrictTh, $"%{query}%") ||
                EF.Functions.ILike(l.ProvinceTh, $"%{query}%") ||
                l.PostalCode.StartsWith(query))
            .Select(l => new
            {
                Location = l,
                Score = Math.Max(
                    EF.Functions.TrigramsSimilarity(l.SubDistrictEn, query),
                    Math.Max(
                        EF.Functions.TrigramsSimilarity(l.DistrictEn, query),
                        Math.Max(
                            EF.Functions.TrigramsSimilarity(l.ProvinceEn, query),
                            Math.Max(
                                EF.Functions.TrigramsSimilarity(l.SubDistrictTh, query),
                                Math.Max(
                                    EF.Functions.TrigramsSimilarity(l.DistrictTh, query),
                                    EF.Functions.TrigramsSimilarity(l.ProvinceTh, query)
                                )
                            )
                        )
                    )
                )
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Location.ProvinceTh)
            .ThenBy(x => x.Location.DistrictTh)
            .ThenBy(x => x.Location.SubDistrictTh)
            .Take(limit)
            .Select(x => x.Location)
            .ToListAsync();

        return results;
    }

    /// <summary>
    /// Performs multi-field fuzzy matching for Thai addresses using all available data.
    /// Returns best matches ranked by composite similarity score.
    /// Weighted scoring: PostalCode: 40%, District: 30%, City: 20%, Province: 10%
    /// </summary>
    /// <param name="postalCode">Postal code to match (5 digits).</param>
    /// <param name="district">Sub-district name in Thai or English.</param>
    /// <param name="city">District/city name in Thai or English.</param>
    /// <param name="province">Province name in Thai or English.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <returns>Locations ranked by composite similarity score.</returns>
    public async Task<IEnumerable<ThaiLocation>> AutocompleteMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3)
    {
        if (string.IsNullOrWhiteSpace(postalCode) &&
            string.IsNullOrWhiteSpace(district) &&
            string.IsNullOrWhiteSpace(city) &&
            string.IsNullOrWhiteSpace(province))
        {
            return Enumerable.Empty<ThaiLocation>();
        }

        const double postalCodeWeight = 0.4;
        const double districtWeight = 0.3;
        const double cityWeight = 0.2;
        const double provinceWeight = 0.1;

        var hasPostalCode = !string.IsNullOrWhiteSpace(postalCode);
        var hasDistrict = !string.IsNullOrWhiteSpace(district);
        var hasCity = !string.IsNullOrWhiteSpace(city);
        var hasProvince = !string.IsNullOrWhiteSpace(province);

        var postalCodeValue = postalCode ?? string.Empty;
        var districtValue = district ?? string.Empty;
        var cityValue = city ?? string.Empty;
        var provinceValue = province ?? string.Empty;

        var results = await _context.ThaiLocations
            .Where(l =>
                (hasPostalCode && EF.Functions.TrigramsAreSimilar(l.PostalCode, postalCodeValue)) ||
                (hasDistrict && (EF.Functions.TrigramsAreSimilar(l.SubDistrictTh, districtValue) ||
                                EF.Functions.TrigramsAreSimilar(l.SubDistrictEn, districtValue))) ||
                (hasCity && (EF.Functions.TrigramsAreSimilar(l.DistrictTh, cityValue) ||
                            EF.Functions.TrigramsAreSimilar(l.DistrictEn, cityValue))) ||
                (hasProvince && (EF.Functions.TrigramsAreSimilar(l.ProvinceTh, provinceValue) ||
                                EF.Functions.TrigramsAreSimilar(l.ProvinceEn, provinceValue))))
            .Select(l => new
            {
                Location = l,
                Score =
                    (hasPostalCode ? postalCodeWeight * EF.Functions.TrigramsSimilarity(l.PostalCode, postalCodeValue) : 0) +
                    (hasDistrict ? districtWeight * Math.Max(
                        EF.Functions.TrigramsSimilarity(l.SubDistrictTh, districtValue),
                        EF.Functions.TrigramsSimilarity(l.SubDistrictEn, districtValue)) : 0) +
                    (hasCity ? cityWeight * Math.Max(
                        EF.Functions.TrigramsSimilarity(l.DistrictTh, cityValue),
                        EF.Functions.TrigramsSimilarity(l.DistrictEn, cityValue)) : 0) +
                    (hasProvince ? provinceWeight * Math.Max(
                        EF.Functions.TrigramsSimilarity(l.ProvinceTh, provinceValue),
                        EF.Functions.TrigramsSimilarity(l.ProvinceEn, provinceValue)) : 0)
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.Location)
            .ToListAsync();

        return results;
    }
}
