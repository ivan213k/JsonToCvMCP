using JsonToCvApi.Models;

namespace JsonToCvApi.Services;

public interface ICvRenderService
{
    Task<RenderedCv> RenderToPdfAsync(CvData cv, CvLanguage language = CvLanguage.En, CancellationToken cancellationToken = default);
}
