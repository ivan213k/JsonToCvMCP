using ZiggyCreatures.Caching.Fusion;

namespace JsonToCvApi.Configuration;

public static class CachingSetup
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>() ?? new CachingOptions();

        services.AddMemoryCache();
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));
        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions { Duration = cachingOptions.RenderedCvDuration });

        return services;
    }
}
