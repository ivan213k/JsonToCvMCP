using JsonToCvApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace JsonToCvApi.Tests.Services;

/// <summary>
/// Exercises the real Playwright renderer end-to-end, not a mock — the point of this class is
/// verifying the headless-shell binary actually launches and produces a valid, correctly-paginated
/// PDF, which no amount of mocking can prove. Requires chromium-headless-shell to already be
/// installed (`playwright install chromium-headless-shell`, or the Dockerfile's build-time step);
/// it is intentionally not downloaded lazily by the test run.
/// </summary>
public class PdfRendererTests : IAsyncLifetime
{
    private readonly PdfRenderer _renderer = new(NullLogger<PdfRenderer>.Instance);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _renderer.DisposeAsync();

    [Fact]
    public async Task RenderToPdfAsync_ProducesValidPdfBytes()
    {
        const string html = "<!doctype html><html><body><h1>Spike check</h1></body></html>";

        var pdf = await _renderer.RenderToPdfAsync(html);

        Assert.NotEmpty(pdf);
        // "%PDF-" magic bytes: proves this is a real PDF, not an error page or empty buffer.
        Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5));
    }

    [Fact]
    public async Task RenderToPdfAsync_HonorsA4PageSizeAndPagination()
    {
        // @page A4 plus enough content to force a second page — proves the A4-paged decision
        // (not single continuous page) actually holds in the rendered output.
        var entries = string.Concat(Enumerable.Range(1, 20)
            .Select(i => $"<div style='break-inside:avoid'><h2>Entry {i}</h2><p>Line one.</p><p>Line two.</p></div>"));
        const string head = "<!doctype html><html><head><style>@page { size: A4; margin: 14mm; }</style></head>";
        var html = $"{head}<body>{entries}</body></html>";

        var pdf = await _renderer.RenderToPdfAsync(html);

        var text = System.Text.Encoding.Latin1.GetString(pdf);
        // A4 width in PDF points (~595.92 — Chromium emits 595.91998, not the nominal 595.28)
        // appears on every page's /MediaBox.
        var mediaBox = System.Text.RegularExpressions.Regex.Match(text, @"MediaBox\s*\[0 0 ([\d.]+) ([\d.]+)\]");
        Assert.True(mediaBox.Success, "Expected a /MediaBox entry.");
        Assert.Equal(595.92, double.Parse(mediaBox.Groups[1].Value), precision: 1);
        // "/Count N" on the document's page tree root is the one page-count signal guaranteed to
        // stay in plain text — individual /Type/Page objects can end up inside a compressed object
        // stream, but the Pages tree node itself doesn't. N > 1 proves the content actually
        // overflowed onto a second A4 page rather than rendering as one long page.
        var match = System.Text.RegularExpressions.Regex.Match(text, @"/Count\s+(\d+)");
        Assert.True(match.Success, "Expected a /Count entry on the page tree root.");
        Assert.True(int.Parse(match.Groups[1].Value) > 1, $"Expected multiple A4 pages, got: {match.Value}");
    }
}
