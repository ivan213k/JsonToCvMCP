using JsonToCvApi.Localization;
using JsonToCvApi.Services;

namespace JsonToCvApi.Configuration;

public static class CvServicesSetup
{
    public static IServiceCollection AddCvServices(this IServiceCollection services)
    {
        services.AddSingleton<IPdfRenderer, PdfRenderer>();
        services.AddSingleton<ICvLocalizationProvider, CvLocalizationProvider>();
        services.AddSingleton<ICvRenderService, CvRenderService>();
        services.AddSingleton<IRenderedCvStore, RenderedCvStore>();

        return services;
    }
}
