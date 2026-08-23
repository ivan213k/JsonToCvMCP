using JsonToCvApi.Models;

namespace JsonToCvApi.Services;

public interface ICvRenderService
{
    Task<byte[]> RenderToPdfAsync(CvData cv, CancellationToken cancellationToken = default);
}
