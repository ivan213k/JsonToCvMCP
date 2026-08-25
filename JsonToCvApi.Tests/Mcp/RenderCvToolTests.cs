using JsonToCvApi.Configuration;
using JsonToCvApi.Mcp;
using JsonToCvApi.Models;
using JsonToCvApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace JsonToCvApi.Tests.Mcp;

public class RenderCvToolTests : IAsyncLifetime
{
    private readonly PdfRenderer _pdfRenderer = new(NullLogger<PdfRenderer>.Instance);
    private CvRenderService _renderService = null!;
    private RenderedCvStore _store = null!;

    public Task InitializeAsync()
    {
        _renderService = new CvRenderService(_pdfRenderer);

        var services = new ServiceCollection();
        services.AddFusionCache();
        var cache = services.BuildServiceProvider().GetRequiredService<IFusionCache>();
        _store = new RenderedCvStore(cache, Options.Create(new CachingOptions()));

        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _pdfRenderer.DisposeAsync();

    [Fact]
    public async Task RenderCvAsync_ReturnsUrl_ThatServesTheSamePdf()
    {
        var cv = new CvData(
            FullName: "Jane Doe",
            Headline: "Engineer",
            Contact: [new ContactItem(ContactKinds.Email, "jane@example.com"), new ContactItem(ContactKinds.Address, "Nowhere")],
            Summary: "Summary text.",
            Skills: ["C#"],
            Experience: [],
            Education: [],
            Languages: [new LanguageEntry("English", "Native")]);

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("cv.example.com") },
            },
        };

        var response = await RenderCvTool.RenderCvAsync(_renderService, _store, httpContextAccessor, cv);

        Assert.StartsWith("https://cv.example.com/api/cv/", response.Url);
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);

        var id = Guid.Parse(response.Url.Split('/')[^1]);
        var storedPdf = await _store.TryGetAsync(id);
        Assert.NotNull(storedPdf);
        Assert.Equal("%PDF-"u8.ToArray(), storedPdf!.Take(5));
    }
}
