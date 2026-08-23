namespace JsonToCvApi.Configuration;

public class CachingOptions
{
    public const string SectionName = "Caching";
    
    public TimeSpan RenderedCvDuration { get; set; } = TimeSpan.FromMinutes(15);
}
