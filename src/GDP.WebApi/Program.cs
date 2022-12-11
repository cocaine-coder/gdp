global using FastEndpoints;
using FastEndpoints.Swagger;
using GDP.Persistence;
using GDP.Services;
using GDP.WebApi.Extensions;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Swagger;
using System.Text.Json;
using WatchDog;
using WatchDog.src.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFastEndpoints(o =>
    {
        o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
    })
    .AddAuthenticationWithJWTBearer(builder.Configuration.GetValue<string>("Security:TokenSigningKey")!, validateLifetime: false)
    .AddWatchDogServices(opt =>
    {
        opt.IsAutoClear = true;
        opt.ClearTimeSchedule = WatchDogAutoClearScheduleEnum.Monthly;
    })
    .AddPersistence(builder.Configuration.GetConnectionString("default")!)
    .AddAppServices();

builder.Services.AddSwaggerDoc(settings: s =>
{
    s.DocumentName = "veersion 1.0";
    s.TypeMappers.AddGeometry(GeoSerializeType.Geojson);
}, serializerSettings: s =>
{
    s.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
}, shortSchemaNames: true);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";

    config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    var jsonSerializerConverters = config.Serializer.Options.Converters;
    jsonSerializerConverters.Add(new GeoJsonConverterFactory());
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
    app.Services.CreateFakeData();
}

app.UseWatchDog(opt =>
{
    opt.WatchPageUsername = "gdp";
    opt.WatchPagePassword = "gdp@123";
});

app.Run();