using Microsoft.Playwright;

namespace JsonToCvApi.Services;

public sealed class PdfRenderer : IPdfRenderer, IAsyncDisposable
{
    private readonly ILogger<PdfRenderer> _logger;
    private readonly SemaphoreSlim _launchGate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PdfRenderer(ILogger<PdfRenderer> logger) => _logger = logger;

    public async Task<byte[]> RenderToPdfAsync(string html, CancellationToken cancellationToken = default)
    {
        var browser = await GetBrowserAsync(cancellationToken);
        await using var page = await browser.NewPageAsync();

        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
        await page.EvaluateAsync("document.fonts.ready");

        return await page.PdfAsync(new PagePdfOptions { Format = "A4", PrintBackground = true });
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsConnected: true }) return _browser;

        await _launchGate.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsConnected: true }) return _browser;

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                ExecutablePath = ResolveHeadlessShellPath(),
                Args = ["--no-sandbox", "--disable-dev-shm-usage"],
            });
            _logger.LogInformation("Launched headless Chromium for PDF rendering.");
            return _browser;
        }
        finally
        {
            _launchGate.Release();
        }
    }

    /// <summary>
    /// Playwright's Chromium.LaunchAsync launches full Chromium by default; headless-shell needs its
    /// executable path given explicitly. Resolved from whichever "chromium_headless_shell-*" install
    /// Playwright placed under PLAYWRIGHT_BROWSERS_PATH (or its default cache dir).
    /// </summary>
    private static string ResolveHeadlessShellPath()
    {
        var browsersRoot = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (string.IsNullOrEmpty(browsersRoot))
        {
            browsersRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "ms-playwright");
        }

        var shellDir = Directory.Exists(browsersRoot)
            ? Directory.GetDirectories(browsersRoot, "chromium_headless_shell-*").FirstOrDefault()
            : null;

        if (shellDir is null)
        {
            throw new InvalidOperationException(
                $"No chromium_headless_shell-* install found under '{browsersRoot}'. Run " +
                "'dotnet JsonToCvApi.dll --playwright-install' (or 'playwright install chromium-headless-shell') first.");
        }

        return Path.Combine(shellDir, "chrome-headless-shell-linux64", "chrome-headless-shell");
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _launchGate.Dispose();
    }
}
