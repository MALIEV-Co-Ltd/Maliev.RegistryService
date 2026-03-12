using Maliev.RegistryService.Infrastructure.Persistence;
using Maliev.RegistryService.Domain.Entities;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Infrastructure.Services;

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
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.ThaiLocations.FindAsync(id);
        if (existing == null) return false;

        _context.ThaiLocations.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

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
            .Take(limit)
            .ToListAsync();
    }

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
            query = query.Where(l => l.DistrictTh.Contains(district) || l.DistrictEn.Contains(district));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.SubDistrictTh.Contains(city) || l.SubDistrictEn.Contains(city));

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(l => l.ProvinceTh.Contains(province) || l.ProvinceEn.Contains(province));

        return await query.OrderBy(l => l.ProvinceTh).ThenBy(l => l.DistrictTh).Take(limit).ToListAsync();
    }
}
