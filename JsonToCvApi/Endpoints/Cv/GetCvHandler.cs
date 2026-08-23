using JsonToCvApi.Services;

namespace JsonToCvApi.Endpoints.Cv;

internal static class GetCvHandler
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        IRenderedCvStore store,
        CancellationToken cancellationToken)
    {
        var pdf = await store.TryGetAsync(id, cancellationToken);
        return pdf is null ? Results.NotFound() : Results.File(pdf, "application/pdf", "cv.pdf");
    }
}
