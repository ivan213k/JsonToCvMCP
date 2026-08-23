using JsonToCvApi.Mcp;

namespace JsonToCvApi.Tests.Mcp;

public class PingToolTests
{
    [Fact]
    public void Ping_ReturnsPong() => Assert.Equal("pong", PingTool.Ping());
}
