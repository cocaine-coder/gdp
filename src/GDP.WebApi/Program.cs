global using System.Data;
global using System.Text.Json;

global using FastEndpoints;
global using FastEndpoints.Swagger;

using GDP.Persistence;
using GDP.Services;
using GDP.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO.Converters;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Serilog.Sinks.PostgreSQL;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var defaultConnectionString = configuration.GetConnectionString("default")!;
        var tokenSigningKey = builder.Configuration.GetValue<string>("App:TokenSigningKey")!;
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
            .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
#endif
            .CreateLogger();

        #endregion create logger

        builder.Host.UseSerilog();

        builder.Services
            .AddFastEndpoints(o =>
            {
                o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
            })
            .AddAuthenticationWithJWTBearer(tokenSigningKey)
            .AddDbContext<GdpDbContext>(options =>
            {
                options.UseNpgsql(defaultConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

                if (builder.Environment.IsDevelopment())
                    options.EnableSensitiveDataLogging();
            })
            .AddTransient<IDbConnection>(serviceProvider =>
            {
                var connection = serviceProvider.GetRequiredService<GdpDbContext>().Database.GetDbConnection();
                //var connection = new Npgsql.NpgsqlConnection(defaultConnectionString);
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                return connection;
            })
            .AddAppServices()
            .AddCors(options =>
            {
                options.AddPolicy("all", policy =>
                {
                    policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            })
            .AddCustomSwaggerDoc();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseCors("all");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = "api";
            config.Versioning.PrependToRoute = true;

            config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var jsonSerializerConverters = config.Serializer.Options.Converters;
            jsonSerializerConverters.Add(new GeoJsonConverterFactory());
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen();
            await app.Services.CreateFakeDataAsync();
        }

        app.Run();
    }
}