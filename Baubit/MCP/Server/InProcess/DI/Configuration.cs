using ModelContextProtocol.Server;
using Baubit.MCP.Server.DI;

namespace Baubit.MCP.Server.InProcess.DI
{
    public record Configuration : AConfiguration
    {
        public string ServerName { get; init; } = "InProc_MCPServer";
    }
}
