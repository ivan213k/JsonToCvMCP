using JsonToCvApi.Configuration;
using JsonToCvApi.Endpoints.Cv;
using JsonToCvApi.Endpoints.Health;

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

builder.Services.AddVersionedOpenApi(semanticVersion);
builder.Services.AddValidation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddCvServices();
builder.Services.AddCvMcp(semanticVersion);

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