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
    public async Task StoreThenGet_ReturnsTheSameBytesAndFileName()
    {
        var store = CreateStore();
        var cv = new RenderedCv([1, 2, 3, 4], "Jane_Doe_CV.pdf");

        var (id, expiresAt) = await store.StoreAsync(cv);
        var fetched = await store.TryGetAsync(id);

        Assert.NotNull(fetched);
        Assert.Equal(cv.Pdf, fetched!.Pdf);
        Assert.Equal(cv.FileName, fetched.FileName);
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
        var (id, _) = await store.StoreAsync(new RenderedCv([1, 2, 3], "CV.pdf"));

        await Task.Delay(50);
        var fetched = await store.TryGetAsync(id);

        Assert.Null(fetched);
    }
}
