namespace JsonToCvApi.Services;

public interface IRenderedCvStore
{
    Task<(Guid Id, DateTimeOffset ExpiresAt)> StoreAsync(RenderedCv cv, CancellationToken cancellationToken = default);

    Task<RenderedCv?> TryGetAsync(Guid id, CancellationToken cancellationToken = default);
}
