# JsonToCvApi

Renders a JSON CV into a PDF via a hand-authored, ATS-optimized HTML template ("Slate"), exposed both
as a REST API and as an in-process MCP server. Headless Chromium (Playwright) does the rendering; a
rendered PDF is cached in memory and served back by URL rather than returned inline.

## Running

```bash
dotnet run --project JsonToCvApi
```

Swagger UI is available at `/swagger` in development.

## API

```bash
# Render a CV, get back { url, expiresAt }
curl -X POST http://localhost:5080/api/cv/render \
  -H "Content-Type: application/json" \
  --data @JsonToCvApi/Data/sample-cv.json

# Fetch the PDF from the url above
curl http://localhost:5080/api/cv/{id} --output cv.pdf
```

`Data/sample-cv.json` is a ready-to-use fixture — POST it as-is to try the full pipeline. See
`Models/CvData.cs` for the input schema.

## MCP

The same functionality is exposed as an MCP server at `/mcp` (stateless Streamable HTTP), with a
`render_cv` tool that returns the same `{ url, expiresAt }` shape as the REST endpoint — never raw PDF
bytes, since a several-hundred-KB blob has no place in an LLM context window.

## Testing

```bash
dotnet test
```

Tests render real PDFs through headless Chromium (not mocks), so the browser binary must be installed
first:

```bash
dotnet run --project JsonToCvApi -- --playwright-install
```

This is a custom mode (`Program.cs`), not a global `playwright` CLI — there isn't one on PATH from the
NuGet package alone. It runs `playwright install --with-deps`, which also apt-installs system libraries
the browser needs, so it requires root; it works as-is in Docker/CI (both run as root), but locally
you'll need passwordless sudo or to run it via `sudo`.

## Docker

```bash
docker build -t jsontocvapi .
docker run -p 8080:8080 jsontocvapi
```

CI builds and publishes `ivan213k/jsontocvapi` on push to `master` (`.github/workflows/docker-publish.yml`).
See `deploy/` for the scripts that pull and run a published image on a server.

## More detail

`CLAUDE.md` has the full design rationale — locked decisions, gotchas, and why things are built the way
they are.
