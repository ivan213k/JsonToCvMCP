using JsonToCvApi.Configuration;
using JsonToCvApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace JsonToCvApi.Tests.Services;

public class RenderedCvStoreTests
{
    private static RenderedCvStore CreateStore(TimeSpan? duration = null)
    {
        var services = new ServiceCollection();
        services.AddFusionCache();
        var cache = services.BuildServiceProvider().GetRequiredService<IFusionCache>();

        var options = Options.Create(new CachingOptions { RenderedCvDuration = duration ?? TimeSpan.FromMinutes(15) });
        return new RenderedCvStore(cache, options);
    }

    [Fact]
    public async Task StoreThenGet_ReturnsTheSameBytes()
    {
        var store = CreateStore();
        byte[] pdf = [1, 2, 3, 4];

        var (id, expiresAt) = await store.StoreAsync(pdf);
        var fetched = await store.TryGetAsync(id);

        Assert.Equal(pdf, fetched);
        Assert.True(expiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNull()
    {
        var store = CreateStore();

        var fetched = await store.TryGetAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task Get_ExpiredEntry_ReturnsNull()
    {
        var store = CreateStore(duration: TimeSpan.FromMilliseconds(1));
        var (id, _) = await store.StoreAsync([1, 2, 3]);

        await Task.Delay(50);
        var fetched = await store.TryGetAsync(id);

        Assert.Null(fetched);
    }
}
