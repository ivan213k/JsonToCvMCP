using JsonToCvApi.Configuration;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace JsonToCvApi.Services;

/// <summary>
/// Stores a rendered PDF under a fresh id and serves it back by that id — what lets
/// <c>POST /api/cv/render</c> return a URL instead of the PDF bytes themselves. Backed by
/// FusionCache, memory-only (no Redis/L2) for now, so an entry doesn't survive a restart or
/// scale-out beyond one instance.
/// </summary>
public sealed class RenderedCvStore : IRenderedCvStore
{
    private readonly IFusionCache _cache;
    private readonly TimeSpan _duration;

    public RenderedCvStore(IFusionCache cache, IOptions<CachingOptions> options)
    {
        _cache = cache;
        _duration = options.Value.RenderedCvDuration;
    }

    public async Task<(Guid Id, DateTimeOffset ExpiresAt)> StoreAsync(byte[] pdf, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        await _cache.SetAsync(CacheKey(id), pdf, _duration, token: cancellationToken);
        return (id, DateTimeOffset.UtcNow.Add(_duration));
    }

    public async Task<byte[]?> TryGetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await _cache.TryGetAsync<byte[]>(CacheKey(id), token: cancellationToken)).GetValueOrDefault();

    private static string CacheKey(Guid id) => $"cv:{id}";
}
