using GDP.Postgis;

namespace GDP.WebApi.Endpoints.Geo;

public class GeobufRequest
{
    public string Schema { get; set; }

    public string Table { get; set; }

    public string GeomCol { get; set; }

    public string IdCol { get; set; } = "id";

    public string? Columns { get; set; }

    public string? Filter { get; set; }
}

public class GeobufEndpoint : Endpoint<GeobufRequest>
{
    public override void Configure()
    {
        Get("geo/buf/{Schema}/{Table}/{GeomCol}/{IdCol}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GeobufRequest req, CancellationToken ct)
    {
        var conn = Resolve<IDbConnection>();
        var buffer = await conn.QueryGeoBufferAsync(req.Schema, req.Table, req.GeomCol, req.IdCol, req.Columns, req.Filter);
        await SendBytesAsync(buffer, contentType: "application/x-protobuf", cancellation: ct);
    }
}