using System.ComponentModel;
using ModelContextProtocol.Server;

namespace JsonToCvApi.Mcp;

/// <summary>
/// Placeholder tool that proves MCP tool discovery and dispatch are wired up.
/// Replaced by the real render tool once the CV pipeline lands.
/// </summary>
[McpServerToolType]
public static class PingTool
{
    [McpServerTool(Name = "ping")]
    [Description("Health check for the JsonToCv MCP server. Returns 'pong'.")]
    public static string Ping() => "pong";
}
