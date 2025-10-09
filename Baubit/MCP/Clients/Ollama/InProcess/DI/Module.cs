using Baubit.Configuration;
using Baubit.DI;
using Baubit.MCP.Server.InProcess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Baubit.MCP.Clients.Ollama.InProcess.DI
{
    public class Module : Ollama.DI.AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        protected override IClientTransport BuildClientTransport(IServiceProvider serviceProvider)
        {
            var tcpLoopback = serviceProvider.GetRequiredService<McpServer>().TCPLoopback;
            return new StreamClientTransport(tcpLoopback.ClientSideStream, tcpLoopback.ClientSideStream, serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
