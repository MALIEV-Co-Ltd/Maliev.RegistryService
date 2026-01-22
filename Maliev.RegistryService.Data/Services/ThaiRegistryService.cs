using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maliev.RegistryService.Data.Services;

public interface IThaiRegistryService
{
    Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit);
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
            ORDER BY search_score DESC
            LIMIT {2}";

        var results = await _context.ThaiLocations
            .FromSqlRaw(textSql, query, $"%{query}%", limit)
            .ToListAsync();

        return results;
    }
}
