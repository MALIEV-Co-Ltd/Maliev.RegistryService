using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Domain.Entities;
using Maliev.RegistryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Infrastructure.Services;

/// <summary>
/// Entity Framework implementation of Thai address registry operations.
/// </summary>
public class ThaiRegistryService : IThaiRegistryService
{
    private const int MaximumPageSize = 100;
    private readonly RegistryDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThaiRegistryService"/> class.
    /// </summary>
    /// <param name="context">Registry database context.</param>
    public ThaiRegistryService(RegistryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<ThaiLocation>> ListAsync(string? query, int pageNumber, int pageSize)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, MaximumPageSize);
        var locationsQuery = _context.ThaiLocations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim();
            locationsQuery = locationsQuery.Where(l =>
                l.DistrictTh.Contains(normalizedQuery) ||
                l.SubDistrictTh.Contains(normalizedQuery) ||
                l.ProvinceTh.Contains(normalizedQuery) ||
                l.DistrictEn.Contains(normalizedQuery) ||
                l.SubDistrictEn.Contains(normalizedQuery) ||
                l.ProvinceEn.Contains(normalizedQuery) ||
                l.PostalCode.Contains(normalizedQuery));
        }

        var totalCount = await locationsQuery.CountAsync();
        var items = await locationsQuery
            .OrderBy(l => l.ProvinceTh)
            .ThenBy(l => l.DistrictTh)
            .ThenBy(l => l.SubDistrictTh)
            .ThenBy(l => l.PostalCode)
            .ThenBy(l => l.Id)
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return new PagedResponse<ThaiLocation>
        {
            Items = items,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalCount = totalCount
        };
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
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.ThaiLocations.FindAsync(id);
        if (existing == null) return false;

        _context.ThaiLocations.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ThaiLocation>> AutocompleteAsync(string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<ThaiLocation>();

        return await _context.ThaiLocations
            .Where(l => l.DistrictTh.Contains(query) ||
                        l.SubDistrictTh.Contains(query) ||
                        l.ProvinceTh.Contains(query) ||
                        l.DistrictEn.Contains(query) ||
                        l.SubDistrictEn.Contains(query) ||
                        l.ProvinceEn.Contains(query) ||
                        l.PostalCode.Contains(query))
            .OrderBy(l => l.ProvinceTh)
            .ThenBy(l => l.DistrictTh)
            .ThenBy(l => l.SubDistrictTh)
            .ThenBy(l => l.PostalCode)
            .ThenBy(l => l.Id)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ThaiLocation>> AutocompleteMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3)
    {
        var query = _context.ThaiLocations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(postalCode))
            query = query.Where(l => l.PostalCode.StartsWith(postalCode));

        if (!string.IsNullOrWhiteSpace(district))
            query = query.Where(l => l.SubDistrictTh.Contains(district) || l.SubDistrictEn.Contains(district));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.DistrictTh.Contains(city) || l.DistrictEn.Contains(city));

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(l => l.ProvinceTh.Contains(province) || l.ProvinceEn.Contains(province));

        return await query.OrderBy(l => l.ProvinceTh).ThenBy(l => l.DistrictTh).Take(limit).ToListAsync();
    }
}
