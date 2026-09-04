# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Commands

```bash
dotnet build                                    # build everything (solution: JsonToCvApi.slnx)
dotnet test                                     # run all tests
dotnet run --project JsonToCvApi                # run the API (Swagger UI at /swagger, MCP at /mcp)
```

`dotnet run` honours `JsonToCvApi/Properties/launchSettings.json`, which pins the port to 5080 — it
overrides `ASPNETCORE_URLS`. Pass `--no-launch-profile` to bind elsewhere.

No lint/format command is configured.

## What this is

An ASP.NET Core minimal API (net10.0) that renders a JSON CV into a PDF via a hardcoded HTML template,
exposed both as REST endpoints and as an in-process MCP server. Patterned on the sibling `JobsProviderMCP`
repo (endpoint/handler/service split, MCP tools as thin wrappers over the same services the REST endpoints
call, `MapMcp("/mcp")` with stateless Streamable HTTP).

Current state: **feature-complete for one template — REST and MCP both wired end-to-end.**
`POST /api/cv/render` (and the equivalent `render_cv` MCP tool, `Mcp/RenderCvTool.cs`) takes a `CvData`
body, renders it via `Templates/Slate/template.html` (a Scriban template, hand-authored from the
reference PDF's design — see below) and `Services/PdfRenderer.cs` (real, Docker-validated), caches the
PDF in memory (FusionCache, `Services/RenderedCvStore.cs`), and returns `{ url, expiresAt }` —
`GET /api/cv/{id}` serves the PDF itself, for both entry points (`Services/CvUrlBuilder.cs` is the one
place the URL shape is built, so REST and MCP can't drift on it). `/health` and the `ping` MCP tool
exist only to prove the MCP transport is wired; neither survives into the finished product.

`Ivan_Zaharuk_.Net_Software_Engineer.pdf` in the repo root is a **design reference, not a conversion
target** — see below.

## Locked design decisions

These were settled up front; don't re-litigate them without being asked.

- **The reference PDF is not converted, it is redesigned.** It is Chromium/Skia output with glyph-by-glyph
  absolute positioning (`Td` offsets per character) and no structure (`Tagged: no`), so no automated
  pdf→html extraction yields a template that can reflow. The HTML template is hand-authored from the
  visual design, and deliberately **improved for ATS parsing** over the original (whose two-column layout
  and letter-spaced small-caps headings parse poorly).
- **A4 paged, not one continuous page.** The reference PDF is 595.92 × 1277.04 pt — A4 width at ~2.15×
  A4 height, a single page that never paginates. The template targets real A4 pages, so print CSS
  (`@page`, `break-inside: avoid` on entries) is load-bearing, not decoration.
- **Renderer is headless Chromium via Playwright .NET** (`Services/PdfRenderer.cs`), specifically
  `chromium-headless-shell`, not full Chromium. Same engine that produced the reference PDF, and the only
  realistic option with full flex/grid + web-font + `@page`/`break-inside` support. Chosen over
  PuppeteerSharp after a Docker spike measured identical render times for both (~130ms cold, ~65ms warm
  for a 4-page A4 doc) — Playwright kept for the .NET-native install/typed API. headless-shell over full
  Chromium because this process only ever needs PDF output, never screenshots/video/a GUI surface: saves
  ~600MB in the image (1.38GB vs 1.97GB).
- **Base image is Ubuntu Noble, not Debian** — this reverses what was assumed going in. `mcr.microsoft.com/
  dotnet/aspnet:10.0`'s only non-Alpine variant turned out to be Ubuntu; .NET 10 dropped Debian GA
  entirely (only a `trixie-slim` *preview* tag exists). This mattered concretely: Ubuntu's `chromium` apt
  package is a snap-transition stub that fails outright in a container (`requires the chromium snap to be
  installed`), so the browser is installed via Playwright's own downloader
  (`dotnet JsonToCvApi.dll --playwright-install`, run at Docker build time — see `Program.cs`'s
  `--playwright-install` branch and the Dockerfile), not apt. `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` is
  still left **off**, unlike `JobsProviderMCP`: CV dates render culture-aware month names
  ("October 2022"), which invariant mode breaks.
- **One `IBrowser` stays alive for the process lifetime** (`PdfRenderer`, singleton in DI), not
  launched per request — a fresh launch costs ~130ms per the spike, wasted if paid every render.
- **Both `POST /api/cv/render` and the `render_cv` MCP tool return a URL, not the PDF bytes.** A base64
  blob of a several-hundred-KB PDF in an LLM context window is unacceptable. The MCP tool is a thin
  wrapper over the same `ICvRenderService`/`IRenderedCvStore` the REST handler calls — no rendering or
  caching logic duplicated between the two entry points, matching the `JobsProviderMCP` pattern. The PDF
  is cached in memory via FusionCache (`Services/RenderedCvStore.cs`, keyed by a fresh `Guid`) and served
  back by `GET /api/cv/{id}`; the response's `expiresAt` reflects `Caching:RenderedCvDuration` (default
  15 min, `appsettings.json`). **No Redis (L2) for now** — deliberately memory-only, so an entry doesn't
  survive a restart or scale-out beyond one instance; add `WithRegisteredDistributedCache()` in
  `Program.cs` if that changes. The MCP tool resolves the current request's `HttpContext` via
  `IHttpContextAccessor` (registered in `Program.cs`) to build the same absolute URL the REST handler
  builds from its bound `HttpRequest` — confirmed live that both land on the same `CvUrlBuilder.Build`.

## CV template and rendering

- **`Templates/Slate/template.html` is a Scriban template**, not a static document — placeholders like
  `{{ FullName }}` and loops like `{{ for exp in Experience }}` bind against `CvRenderService`'s view
  model. "Slate" is this design's name — one hardcoded template today, but each template gets its own
  `Templates/{Name}/` folder (markup + its own `Fonts/`) so a second one later is a new folder, not a
  rework of this one. `Templates/SlateTemplate.cs` only handles font embedding (see below); it has no
  idea the file contains Scriban syntax, and exposes the result as `Shell`, deliberately not `Html` —
  it's not valid standalone HTML until `CvRenderService` binds a model to it.
- **The input model (`Models/CvData.cs`) is tailored to this template, not JSON Resume.** JSON Resume
  was considered (a de facto standard, so other tooling/LLM clients likely already know its shape) but
  its `work` entries don't cleanly hold `ExperienceEntry.ProjectName`/`ProjectDescription`, which the
  template's design needs. Revisit only if multi-template support is ever added — a shared schema
  matters more once there's more than one template consuming it.
- **Scriban does not auto-escape HTML.** `{{ }}` is a raw text-templating engine — `html.escape` is an
  opt-in filter Scriban itself provides, not automatic — so `CvRenderService` HTML-encodes every
  free-text field itself, once, before it ever reaches the template (see the class's own doc comment).
  Verified live: a `<script>` tag and a `javascript:` LinkedIn URL both come back inert (see git history
  around the escaping tests in `CvRenderServiceTests.cs` for the exact payloads). Do not add
  interpolations to the template that bypass this — they'd be unescaped by default.
- **Fonts (Roboto, Libre Bodoni) are embedded as base64 `@font-face` data URIs**, assembled into the
  template at runtime by `SlateTemplate.cs` from four `.woff2` files in `Templates/Slate/Fonts/`. Those
  files are **static (non-variable) instances**, not what Google Fonts serves by default: Chromium's PDF export
  rasterizes a variable-weight font's glyphs as Type 3 (bitmap-like) rather than proper CID TrueType
  outlines — confirmed by comparing `pdffonts` output on a first draft (Type 3) against the reference
  PDF's own embedded Roboto (CID TrueType) — which is worse for ATS text-layer reliability. The static
  instances were produced from the Google Fonts variable sources via `fontTools.varLib.instancer` at
  wght=400/700, run once in a throwaway container (not part of the build); regenerate only if the
  typefaces change.
- **Date formatting uses an explicit `en-US` culture, not `CultureInfo.CurrentCulture`.** Leaving
  globalization non-invariant (see below) is what makes "October 2022" possible at all, but the
  container's default locale shouldn't be what decides it — the document should render in English
  regardless of host locale. Done in `CvRenderService`, not the template — Scriban has no clean
  `DateOnly` formatting story, so dates are pre-formatted into display strings before the view model
  is built.

## Gotchas inherited from JobsProviderMCP

- MCP tool parameters that are optional **must have C# default values** (`= null` / `= 0`), not merely
  nullable types. The MCP SDK derives its "required" schema from the presence of a default, so an omitted
  argument fails inside the SDK before the tool method runs.
- `builder.Services.AddValidation()` applies to minimal-API endpoints only. MCP tools bind through the SDK's
  own dispatch and never hit that filter, so any validation that must hold for both entry points belongs in
  the service layer.

## CI and deployment

Patterned on `JobsProviderMCP`'s setup, minus what doesn't apply here (no Redis, so no `deploy/redis/`).

- **Telemetry (`Configuration/TelemetrySetup.cs`)** — OpenTelemetry logging/tracing/metrics exported via
  `UseOtlpExporter()` (ASP.NET Core + HttpClient + runtime + FusionCache instrumentation). Ships to a
  shared OTLP collector rather than one dedicated to this app, so there's no `docker-compose.otel.yml`
  here — `deploy/deploy.sh` joins the `jobsprovider-net` Docker network and defaults
  `OTEL_EXPORTER_OTLP_ENDPOINT` to `http://aspire-dashboard:18889`, overridable via `deploy/.env`. Local
  dev points at `http://localhost:4317` via `appsettings.Development.json`. Services are told apart by
  the `AddService("JsonToCvApi", ...)` resource name.

- **`.github/workflows/pr-validation.yml`** — restore, build, test on every PR. **One real difference
  from `JobsProviderMCP`'s copy of this workflow**: this project's tests render actual PDFs through
  headless Chromium (`PdfRendererTests`, `CvRenderServiceTests`, `RenderCvToolTests` all exercise the
  real `PdfRenderer`, not a fake), so the runner needs the browser binary before `dotnet test` runs —
  hence the `--playwright-install` step, absent from the sibling repo's workflow because its tests don't
  touch a browser at all. Confirmed this actually works, not just assumed: simulated the exact
  restore/build/playwright-install/test sequence in a clean `mcr.microsoft.com/dotnet/sdk:10.0` container
  (no pre-existing Playwright cache) before this was considered done — all 10 tests passed cold.
- **`.github/workflows/docker-publish.yml`** — on push to `master` (this repo's default branch — note
  `JobsProviderMCP`'s copy triggers on `main`, its default branch; don't copy that trigger verbatim into
  other repos), computes a semver via GitVersion from commit messages, builds and pushes
  `ivan213k/jsontocvapi:{semver,latest}` to Docker Hub. Needs `DOCKERHUB_USERNAME`/`DOCKERHUB_TOKEN`
  repo secrets, not yet configured.
- **`GitVersion.yaml`** — commit-message-driven semver bump rules (`feat:`/`fix:` prefixes, `+semver:`
  trailers); copied verbatim from `JobsProviderMCP`, project-agnostic.
- **`deploy/`** — server-side scripts, meant to be copied to a deploy host via `copy-to-server.sh` (run
  locally) and then run *there*: `deploy.sh` pulls the latest (or a given) tag from Docker Hub and runs
  it as a container; `watch.sh` polls Docker Hub for a newer tag than `.current-version` and calls
  `deploy.sh` when one shows up — meant to run on a cron (e.g. `*/5 * * * *`) so new pushes to `master`
  roll out without a manual step on the server. No secrets are passed to the container currently — unlike
  `JobsProviderMCP`'s `deploy.sh` (which threads through a Redis connection string and an Apify token),
  this app has none yet; `deploy/.env` (gitignored) is still read if present, for a future need (e.g. a
  `HOST_PORT` override) without requiring a script change.
