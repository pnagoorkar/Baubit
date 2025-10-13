using Baubit.Bootstrapping;
using Baubit.Configuration;
using Baubit.MCP.Clients.DI;
using Baubit.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OllamaSharp;
using Baubit.DI;
using ModelContextProtocol.Protocol;
using Baubit.MCP.Server.InProcess;

namespace Baubit.MCP.Clients.Ollama.DI
{
    public abstract class AModule<TConfiguration> : AModule<Agent, TConfiguration> where TConfiguration : AConfiguration
    {
        public AModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public AModule(IConfiguration configuration) : base(configuration)
        {
        }

        public AModule(TConfiguration configuration, List<IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        protected override Agent BuildAgent(IServiceProvider serviceProvider)
        {
            return new Agent(serviceProvider.GetRequiredService<IChatClient>(),
                             serviceProvider.GetRequiredService<ChatOptions>(),
                             serviceProvider.GetRequiredService<IHub>(),
                             serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        protected override IChatClient BuildInnerClient(IServiceProvider serviceProvider)
        {
            return new OllamaApiClient(Configuration.OllamaApiClientConfig);
        }

        private async Task<List<McpClientTool>> GetToolsAsync(IServiceProvider serviceProvider)
        {
            var tools = new List<McpClientTool>();
            foreach (var mcpClient in serviceProvider.GetRequiredService<IEnumerable<McpClient>>())
            {
                tools.AddRange(await mcpClient.ListToolsAsync().ConfigureAwait(false));
            }
            return tools;
        }
    }
}
