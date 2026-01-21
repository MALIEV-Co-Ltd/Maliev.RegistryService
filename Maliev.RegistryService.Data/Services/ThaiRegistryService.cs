using Maliev.RegistryService.Data.Context;
using Maliev.RegistryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maliev.RegistryService.Data.Services;

public interface IThaiRegistryService
{
    Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit);
}

public class ThaiRegistryService : IThaiRegistryService
{
    private readonly RegistryDbContext _context;

    public ThaiRegistryService(RegistryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<ThaiLocation>();

        bool isNumeric = query.All(char.IsDigit);
        bool isThai = query.Any(c => c >= 0x0E00 && c <= 0x0E7F);

        // We use pg_trgm similarity() for ranking if needed, 
        // but simple filtering with prioritization as per requirements:
        // 1. Postal Code (if numeric)
        // 2. English match
        // 3. Thai match

        var baseQuery = _context.ThaiLocations.AsNoTracking();

        if (isNumeric)
        {
            return await baseQuery
                .Where(l => l.PostalCode.StartsWith(query))
                .OrderBy(l => l.PostalCode)
                .Take(limit)
                .ToListAsync();
        }

        // For text search, we use OR logic across fields but prioritize English per requirements
        // Note: In a real high-performance scenario, we'd use raw SQL for similarity ordering
        var results = await baseQuery
            .Where(l => 
                EF.Functions.ILike(l.SubDistrictEn, $"%{query}%") ||
                EF.Functions.ILike(l.DistrictEn, $"%{query}%") ||
                EF.Functions.ILike(l.ProvinceEn, $"%{query}%") ||
                EF.Functions.ILike(l.SubDistrictTh, $"%{query}%") ||
                EF.Functions.ILike(l.DistrictTh, $"%{query}%") ||
                EF.Functions.ILike(l.ProvinceTh, $"%{query}%"))
            .ToListAsync();

        return results
            .OrderByDescending(l => IsEnglishMatch(l, query))
            .ThenByDescending(l => IsThaiMatch(l, query))
            .Take(limit);
    }

    private bool IsEnglishMatch(ThaiLocation l, string query) =>
        l.SubDistrictEn.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        l.DistrictEn.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        l.ProvinceEn.Contains(query, StringComparison.OrdinalIgnoreCase);

    private bool IsThaiMatch(ThaiLocation l, string query) =>
        l.SubDistrictTh.Contains(query) ||
        l.DistrictTh.Contains(query) ||
        l.ProvinceTh.Contains(query);
}
