using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maliev.RegistryService.Data.Services;

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

public class ThaiRegistryService : IThaiRegistryService
{
    private readonly RegistryDbContext _context;

    public ThaiRegistryService(RegistryDbContext context)
    {
        _context = context;
    }

    public async Task<ThaiLocation?> GetByIdAsync(Guid id)
    {
        return await _context.ThaiLocations.FindAsync(id);
    }

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

    public async Task<bool> UpdateAsync(ThaiLocation location)
    {
        var existing = await _context.ThaiLocations.FindAsync(location.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(location);
        await _context.SaveChangesAsync();
        
        // Reload to get any database-generated values or ensure state is fresh
        await _context.Entry(existing).ReloadAsync();
        
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var location = await _context.ThaiLocations.FindAsync(id);
        if (location == null) return false;

        _context.ThaiLocations.Remove(location);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<ThaiLocation>();

        if (query.All(char.IsDigit))
        {
            // If it's a postal code, prioritize that search
            var postalSql = @"
                SELECT *
                FROM ""ThaiLocations""
                WHERE ""PostalCode"" LIKE {0}
                ORDER BY ""PostalCode""
                LIMIT {1}";
            
            return await _context.ThaiLocations
                .FromSqlRaw(postalSql, $"{query}%", limit)
                .ToListAsync();
        }

        // For text search, use pg_trgm similarity() for ranking.
        // Leverage greatest similarity across subdistrict, district, province in both languages.
        var textSql = @"
            SELECT *,
                   GREATEST(
                       similarity(""SubDistrictEn"", {0}),
                       similarity(""DistrictEn"", {0}),
                       similarity(""ProvinceEn"", {0}),
                       similarity(""SubDistrictTh"", {0}),
                       similarity(""DistrictTh"", {0}),
                       similarity(""ProvinceTh"", {0})
                   ) as search_score
            FROM ""ThaiLocations""
            WHERE ""SubDistrictEn"" ILIKE {1}
               OR ""DistrictEn"" ILIKE {1}
               OR ""ProvinceEn"" ILIKE {1}
               OR ""SubDistrictTh"" ILIKE {1}
               OR ""DistrictTh"" ILIKE {1}
               OR ""ProvinceTh"" ILIKE {1}
               OR ""PostalCode"" LIKE {1}
            ORDER BY search_score DESC, ""ProvinceTh"", ""DistrictTh"", ""SubDistrictTh""
            LIMIT {2}";

        var results = await _context.ThaiLocations
            .FromSqlRaw(textSql, query, $"%{query}%", limit)
            .ToListAsync();

        return results;
    }

    /// <summary>
    /// Performs multi-field fuzzy matching for Thai addresses using all available data.
    /// Returns best matches ranked by composite similarity score.
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
        // Return empty if all fields are null/empty
        if (string.IsNullOrWhiteSpace(postalCode) && 
            string.IsNullOrWhiteSpace(district) && 
            string.IsNullOrWhiteSpace(city) && 
            string.IsNullOrWhiteSpace(province))
        {
            return Enumerable.Empty<ThaiLocation>();
        }

        // Build scoring SQL that weights fields by reliability
        // Postal code: 40%, District: 30%, City: 20%, Province: 10%
        var sql = @"
            SELECT *,
                   (
                       COALESCE(CASE WHEN {0} IS NOT NULL AND {0} != ''
                           THEN 0.4 * similarity(""PostalCode"", {0}) 
                           ELSE 0 END, 0) +
                       COALESCE(CASE WHEN {1} IS NOT NULL AND {1} != ''
                           THEN 0.3 * GREATEST(
                               similarity(""SubDistrictTh"", {1}),
                               similarity(""SubDistrictEn"", {1})
                           ) 
                           ELSE 0 END, 0) +
                       COALESCE(CASE WHEN {2} IS NOT NULL AND {2} != ''
                           THEN 0.2 * GREATEST(
                               similarity(""DistrictTh"", {2}),
                               similarity(""DistrictEn"", {2})
                           ) 
                           ELSE 0 END, 0) +
                       COALESCE(CASE WHEN {3} IS NOT NULL AND {3} != ''
                           THEN 0.1 * GREATEST(
                               similarity(""ProvinceTh"", {3}),
                               similarity(""ProvinceEn"", {3})
                           ) 
                           ELSE 0 END, 0)
                   ) as composite_score
            FROM ""ThaiLocations""
            WHERE ({0} IS NULL OR {0} = '' OR ""PostalCode"" % {0})
               OR ({1} IS NULL OR {1} = '' OR ""SubDistrictTh"" % {1} OR ""SubDistrictEn"" % {1})
               OR ({2} IS NULL OR {2} = '' OR ""DistrictTh"" % {2} OR ""DistrictEn"" % {2})
               OR ({3} IS NULL OR {3} = '' OR ""ProvinceTh"" % {3} OR ""ProvinceEn"" % {3})
            ORDER BY composite_score DESC
            LIMIT {4}";
        
        var results = await _context.ThaiLocations
            .FromSqlRaw(sql, 
                string.IsNullOrWhiteSpace(postalCode) ? string.Empty : postalCode, 
                string.IsNullOrWhiteSpace(district) ? string.Empty : district, 
                string.IsNullOrWhiteSpace(city) ? string.Empty : city, 
                string.IsNullOrWhiteSpace(province) ? string.Empty : province, 
                limit)
            .ToListAsync();
        
        return results;
    }
}
