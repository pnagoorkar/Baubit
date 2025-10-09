using Baubit.Bootstrapping;
using Baubit.Events;

namespace Baubit.MCP.Clients
{
    public interface IAgent : IAsyncRequestHandler<AgentRequest, AgentResponse>, IBootstrap
    {
    }
}
