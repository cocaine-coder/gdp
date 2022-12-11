using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace GDP.Persistence;

public static class PersistenExtension
{
    /// <summary>
    /// add efcore to di
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GdpDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite());
            options.EnableSensitiveDataLogging();
        })
        .AddScoped<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<GdpDbContext>().Database.GetDbConnection());
        return services;
    }

    /// <summary>
    /// create fake data (only use in test or dev)
    /// </summary>
    /// <param name="provider"></param>
    public static void CreateFakeData(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<GdpDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
}