using Maliev.RegistryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.RegistryService.Data.Context;

/// <summary>
/// Database context for the Registry Service.
/// </summary>
public class RegistryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public RegistryDbContext(DbContextOptions<RegistryDbContext> options) : base(options)
    {
    }

    /// <summary>Gets or sets the Thai locations dataset.</summary>
    public DbSet<ThaiLocation> ThaiLocations => Set<ThaiLocation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<ThaiLocation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PostalCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.SubDistrictTh).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DistrictTh).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProvinceTh).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SubDistrictEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DistrictEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProvinceEn).HasMaxLength(200).IsRequired();

            // B-Tree index for PostalCode
            entity.HasIndex(e => e.PostalCode);

            // GIN Trigram indices for searchable fields
            entity.HasIndex(e => e.SubDistrictTh).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(e => e.DistrictTh).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(e => e.ProvinceTh).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(e => e.SubDistrictEn).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(e => e.DistrictEn).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(e => e.ProvinceEn).HasMethod("gin").HasOperators("gin_trgm_ops");
        });
    }
}
