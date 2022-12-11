global using FastEndpoints;
using FastEndpoints.Swagger;
using GDP.Persistence;
using GDP.Services;
using GDP.WebApi.Extensions;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Swagger;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Serilog.Sinks.PostgreSQL;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var defaultConnectionString = configuration.GetConnectionString("default")!;
var tokenSigningKey = builder.Configuration.GetValue<string>("Security:TokenSigningKey")!;
var logTableName = builder.Configuration.GetValue<string>("App:LogTableName")!;

#region create logger

IDictionary<string, ColumnWriterBase> columnOptions = new Dictionary<string, ColumnWriterBase>
{
    { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
    { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
    { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
    { "raise_date", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
    { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
    { "properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
    { "props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
    { "machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
};

Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(config =>
    {
        config.PostgreSQL(defaultConnectionString,
                          logTableName,
                          columnOptions: columnOptions,
                          restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                          needAutoCreateTable: true);
    })
#if DEBUG
    .WriteTo.Console()
#endif
    .CreateLogger();

#endregion create logger

builder.Host.UseSerilog();

builder.Services
    .AddFastEndpoints(o =>
    {
        o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
    })
    .AddAuthenticationWithJWTBearer(tokenSigningKey, validateLifetime: false)
    .AddDbContext<GdpDbContext>(options =>
    {
        options.UseNpgsql(defaultConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging();
    })
    .AddTransient<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<GdpDbContext>().Database.GetDbConnection())
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

app.Run();