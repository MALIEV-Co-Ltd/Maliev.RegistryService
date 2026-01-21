using Maliev.RegistryService.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Data.Seeding;

public interface IThaiLocationSeeder
{
    Task SeedAsync();
}

public class ThaiLocationSeeder : IThaiLocationSeeder
{
    private readonly RegistryDbContext _context;
    private readonly ILogger<ThaiLocationSeeder> _logger;
    private const string SeedFilePath = "SeedData/thai_locations.sql";

    public ThaiLocationSeeder(RegistryDbContext context, ILogger<ThaiLocationSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.ThaiLocations.AnyAsync())
        {
            _logger.LogInformation("Thai location data already exists. Skipping seeding.");
            return;
        }

        if (!File.Exists(SeedFilePath))
        {
            _logger.LogWarning("Seed file not found at {Path}. Skipping seeding.", SeedFilePath);
            return;
        }

        try
        {
            _logger.LogInformation("Starting Thai location data seeding from SQL...");
            var sql = await File.ReadAllTextAsync(SeedFilePath);
            
            await _context.Database.ExecuteSqlRawAsync(sql);
            
            _logger.LogInformation("Successfully seeded Thai location records from SQL.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Thai location seeding.");
        }
    }
}
