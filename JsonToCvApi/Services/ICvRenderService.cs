using JsonToCvApi.Models;

namespace JsonToCvApi.Services;

public interface ICvRenderService
{
    Task<byte[]> RenderToPdfAsync(CvData cv, CvLanguage language = CvLanguage.En, CancellationToken cancellationToken = default);
}
