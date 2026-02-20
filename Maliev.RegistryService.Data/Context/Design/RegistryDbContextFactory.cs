using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.RegistryService.Data.Context.Design;

/// <summary>
/// Design-time factory for RegistryDbContext.
/// Used by EF Core Tools (migrations) to create context without running the full API host.
/// </summary>
public class RegistryDbContextFactory : IDesignTimeDbContextFactory<RegistryDbContext>
{
    public RegistryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RegistryDbContext>();

        // Use a dummy connection string for design-time operations (migration generation).
        // Actual connection strings are provided at runtime via Aspire/Environment variables.
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time_dummy;Username=postgres;Password=postgres");

        return new RegistryDbContext(optionsBuilder.Options);
    }
}
