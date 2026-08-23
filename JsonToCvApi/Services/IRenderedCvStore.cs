namespace JsonToCvApi.Services;

public interface IRenderedCvStore
{
    Task<(Guid Id, DateTimeOffset ExpiresAt)> StoreAsync(byte[] pdf, CancellationToken cancellationToken = default);

    Task<byte[]?> TryGetAsync(Guid id, CancellationToken cancellationToken = default);
}
