using JsonToCvApi.Services;

namespace JsonToCvApi.Endpoints.Cv;

internal static class GetCvHandler
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        IRenderedCvStore store,
        CancellationToken cancellationToken)
    {
        var cv = await store.TryGetAsync(id, cancellationToken);
        return cv is null ? Results.NotFound() : Results.File(cv.Pdf, "application/pdf", cv.FileName);
    }
}
