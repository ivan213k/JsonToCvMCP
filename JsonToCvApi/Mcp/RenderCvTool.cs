using System.ComponentModel;
using JsonToCvApi.Models;
using JsonToCvApi.Services;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace JsonToCvApi.Mcp;

[McpServerToolType]
public static class RenderCvTool
{
    [McpServerTool(Name = "render_cv")]
    [Description("""
        Renders a CV to PDF from the hardcoded "Slate" template and returns a URL to fetch it. 
        The URL expires; see `expiresAt` on the response. Same underlying render/cache as
        `POST /api/cv/render` and `GET /api/cv/{id}`.
        """)]
    public static async Task<RenderedCvResponse> RenderCvAsync(
        ICvRenderService renderService,
        IRenderedCvStore store,
        IHttpContextAccessor httpContextAccessor,
        [Description("The CV content to render.")] CvData cv,
        CancellationToken cancellationToken = default)
    {
        var pdf = await renderService.RenderToPdfAsync(cv, cancellationToken);
        var (id, expiresAt) = await store.StoreAsync(pdf, cancellationToken);

        var request = httpContextAccessor.HttpContext!.Request;
        return new RenderedCvResponse(CvUrlBuilder.Build(request, id), expiresAt);
    }
}
