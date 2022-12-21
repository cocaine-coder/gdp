namespace GDP.WebApi.Endpoints.V2;

public class TestEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("test");
        AllowAnonymous();
        Version(2);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync("test", cancellation: ct);
    }
}