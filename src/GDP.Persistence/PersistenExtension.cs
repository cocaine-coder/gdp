using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace GDP.Persistence;

public static class PersistenExtension
{
    /// <summary>
    /// create fake data (only use in test or dev)
    /// </summary>
    /// <param name="provider"></param>
    public static void CreateFakeData(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<GdpDbContext>();
        dbContext.Database.EnsureCreated();
    }
}