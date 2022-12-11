using Microsoft.EntityFrameworkCore;

namespace GDP.Persistence;

public class GdpDbContext : DbContext
{
    public GdpDbContext(DbContextOptions<GdpDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}