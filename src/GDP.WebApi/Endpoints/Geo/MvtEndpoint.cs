using GDP.Postgis;

namespace GDP.WebApi.Endpoints.Geo;

public class MvtRequest
{
    public string Schema { get; set; }

    public string Table { get; set; }

    public string GeomCol { get; set; }

    public int Z { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public string IdCol { get; set; } = "id";

    public string? Columns { get; set; }

    public string? Filter { get; set; }
}

public class MvtEndpoint : Endpoint<MvtRequest>
{
    public override void Configure()
    {
        Get("geo/mvt/{Schema}/{Table}/{GeomCol}/{Z}/{X}/{Y}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(MvtRequest req, CancellationToken ct)
    {
        var conn = Resolve<IDbConnection>();
        var buffer = await conn.QueryMvtBufferAsync(req.Schema, req.Table, req.GeomCol, req.IdCol, req.Z, req.X, req.Y, columns: req.Columns,req.Filter);
        await SendBytesAsync(buffer, contentType: "application/x-protobuf", cancellation: ct);
    }
}

public class MvtEndpointV2 : Endpoint<MvtRequest>
{
    public override void Configure()
    {
        Get("geo/mvt/{Schema}/{Table}/{GeomCol}/{Z}/{X}/{Y}");
        AllowAnonymous();
        Version(2);
    }

    public override async Task HandleAsync(MvtRequest req, CancellationToken ct)
    {
        var conn = Resolve<IDbConnection>();
        var buffer = await conn.QueryMvtBufferV2Async(req.Schema, req.Table, req.GeomCol, req.IdCol, req.Z, req.X, req.Y, columns: req.Columns, req.Filter);
        await SendBytesAsync(buffer, contentType: "application/x-protobuf", cancellation: ct);
    }
}