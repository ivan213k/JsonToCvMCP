namespace JsonToCvApi.Endpoints.Health;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth")
            .WithSummary("Liveness probe.")
            .WithDescription("Returns 200 while the API is able to serve requests.")
            .WithTags("Health");

        return app;
    }
}
