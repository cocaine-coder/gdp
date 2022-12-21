using NetTopologySuite.Swagger;
using System.Text.Json;

namespace GDP.WebApi.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddCustomSwaggerDoc(this IServiceCollection services)
    {
        return services

            .AddSwaggerDoc(settings: s =>
            {
                s.DocumentName = "api version 1.0";
                s.Version = "1.0";
                s.TypeMappers.AddGeometry(GeoSerializeType.Geojson);
            }, serializerSettings: s =>
            {
                s.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }, addJWTBearerAuth: true)

            .AddSwaggerDoc(settings: s =>
            {
                s.DocumentName = "api version 2.0";
                s.Version = "2.0";
                s.TypeMappers.AddGeometry(GeoSerializeType.Geojson);
            }, serializerSettings: s =>
            {
                s.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }, addJWTBearerAuth: true, maxEndpointVersion: 2);
    }
}