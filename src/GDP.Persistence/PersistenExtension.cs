using Dapper;
using GDP.Postgis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GDP.Persistence;

public static class PersistenExtension
{
    /// <summary>
    /// create fake data (only use in test or dev)
    /// </summary>
    /// <param name="provider"></param>
    public static async Task CreateFakeDataAsync(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<GdpDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var conn = dbContext.Database.GetDbConnection();

        await conn.CreateSchemaAsync("test");
        await conn.CreateGeoTableAsync("test", "table1");

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GDP.Persistence.SeedDatas.test.geojson");
        using var streamReader = new StreamReader(stream);
        var geojsonString = await streamReader.ReadToEndAsync();

        var fc = JsonSerializer.Deserialize<FeatureCollection>(geojsonString, new JsonSerializerOptions
        {
            Converters = { new GeoJsonConverterFactory() }
        });

        var sqlSB = new StringBuilder("insert into test.table1(geom,properties) values ");

        for (int i = 0; i < fc.Count; i++)
        {
            var feature = fc[i];
            feature.Geometry.SRID = 4326;
            sqlSB.Append($"('{feature.Geometry.ToString()}','{feature.Attributes.GetOptionalValue("properties").ToString()}'::jsonb)");
            if (i != fc.Count - 1)
            {
                sqlSB.Append(',');
            }
        }

        var sql = sqlSB.ToString();
        await conn.ExecuteAsync(sql);
    }
}