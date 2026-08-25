using JsonToCvApi.Models;
using JsonToCvApi.Services;

namespace JsonToCvApi.Endpoints.Cv;

internal static class RenderCvHandler
{
    public static async Task<IResult> HandleAsync(
        CvData cv,
        HttpRequest request,
        ICvRenderService renderService,
        IRenderedCvStore store,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var pdf = await renderService.RenderToPdfAsync(cv, CvLanguageParser.Parse(language), cancellationToken);
        var (id, expiresAt) = await store.StoreAsync(pdf, cancellationToken);

        return Results.Ok(new RenderedCvResponse(CvUrlBuilder.Build(request, id), expiresAt));
    }
}
