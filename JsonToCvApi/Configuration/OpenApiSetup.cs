namespace JsonToCvApi.Configuration;

public static class OpenApiSetup
{
    public static IServiceCollection AddVersionedOpenApi(this IServiceCollection services, string version)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Version = version;
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
