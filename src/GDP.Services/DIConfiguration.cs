using MaxRev.Gdal.Core;
using Microsoft.Extensions.DependencyInjection;
using OSGeo.GDAL;

namespace GDP.Services;

public class DIConfiguration
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        GdalBase.ConfigureAll();
        Gdal.SetConfigOption("DXF_ENCODING", "UTF-8");

        return services;
    }
}