using ModelContextProtocol.Protocol;

namespace JsonToCvApi.Configuration;

public static class McpSetup
{
    public static IServiceCollection AddCvMcp(this IServiceCollection services, string version)
    {
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation { Name = "JsonToCvApi", Version = version };
        }).WithHttpTransport(options => options.Stateless = true).WithToolsFromAssembly();

        return services;
    }
}
