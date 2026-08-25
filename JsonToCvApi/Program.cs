using JsonToCvApi.Configuration;
using JsonToCvApi.Endpoints.Cv;
using JsonToCvApi.Endpoints.Health;
using JsonToCvApi.Localization;
using JsonToCvApi.Services;
using ModelContextProtocol.Protocol;
using ZiggyCreatures.Caching.Fusion;

// Build-time-only mode: installs the headless-shell Chromium binary this process renders PDFs
// with. Invoked from the Dockerfile ("dotnet JsonToCvApi.dll --playwright-install") so the ~250MB
// download happens once at image build, not on a container's first request. Exits immediately
// after, same as `playwright install` itself would — it never reaches WebApplication.CreateBuilder.
if (args.Contains("--playwright-install"))
{
    Microsoft.Playwright.Program.Main(["install", "--with-deps", "chromium-headless-shell"]);
    return;
}

var builder = WebApplication.CreateBuilder(args);

var semanticVersion = Environment.GetEnvironmentVariable("SemanticVersion") ?? "dev";

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Version = semanticVersion;
        return Task.CompletedTask;
    });
});

builder.Services.AddValidation();
builder.Services.AddHttpContextAccessor();

var cachingOptions = builder.Configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>() ?? new CachingOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<CachingOptions>(builder.Configuration.GetSection(CachingOptions.SectionName));
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions { Duration = cachingOptions.RenderedCvDuration });

builder.Services.AddSingleton<IPdfRenderer, PdfRenderer>();
builder.Services.AddSingleton<ICvLocalizationProvider, CvLocalizationProvider>();
builder.Services.AddSingleton<ICvRenderService, CvRenderService>();
builder.Services.AddSingleton<IRenderedCvStore, RenderedCvStore>();

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation { Name = "JsonToCvApi", Version = semanticVersion };
}).WithHttpTransport(options => options.Stateless = true).WithToolsFromAssembly();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", $"JsonToCvApi {semanticVersion}");
});

app.MapHealthEndpoint();
app.MapCvEndpoint();
app.MapMcp("/mcp");

app.Run();

public partial class Program;
