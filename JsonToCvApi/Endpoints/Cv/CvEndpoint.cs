using JsonToCvApi.Models;

namespace JsonToCvApi.Endpoints.Cv;

public static class CvEndpoint
{
    public static IEndpointRouteBuilder MapCvEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cv/render", RenderCvHandler.HandleAsync)
            .WithName("RenderCv")
            .WithTags("Cv")
            .WithSummary("Render a CV to PDF from the hardcoded template.")
            .WithDescription("""
                Binds the request body into the single hardcoded HTML template and renders it to a PDF via
                headless Chromium. Returns a URL the PDF can be fetched from (`GET /api/cv/{id}`), not the
                PDF bytes themselves — a several-hundred-KB body isn't something every caller wants inline.
                The PDF is cached in memory and the URL expires; see `expiresAt` on the response.
                Accepts an optional `language` query parameter (`en`, `de`, `ua`, `ru`, `es`; defaults to
                `en`) that translates the template's own headings/labels and month names — CV content
                itself is rendered as supplied, not translated.
                """)
            .Produces<RenderedCvResponse>()
            .ProducesValidationProblem();

        app.MapGet("/api/cv/{id:guid}", GetCvHandler.HandleAsync)
            .WithName("GetCv")
            .WithTags("Cv")
            .WithSummary("Fetch a previously rendered PDF by id.")
            .WithDescription("""
                Cache-only: only returns a PDF that has previously been rendered by `POST /api/cv/render`
                and whose entry hasn't expired. Returns 404 otherwise — there is no fallback re-render.
                """)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
