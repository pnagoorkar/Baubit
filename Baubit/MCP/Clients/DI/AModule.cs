using Baubit.Bootstrapping;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using System;

namespace Baubit.MCP.Clients.DI
{
    public abstract class AModule<TAgent, TConfiguration> : Baubit.DI.AModule<TConfiguration> where TAgent : class, IAgent where TConfiguration : AConfiguration
    {
        protected AModule(Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected AModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected AModule(TConfiguration configuration, List<Baubit.DI.IModule> nestedModules, List<Baubit.DI.IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            // build the MCP client first, so AI tools are made available first
            services.AddSingleton<McpClient>(BuildMcpClient);
            // then build the chat client with UseFunctionInvocation
            services.AddSingleton(BuildChatClient);
            // then build the chat options that will be used during the chat session
            services.AddSingleton<ChatOptions>(CreateChatOptions);
            // then build the agent that will recieve the chat client and tools for use during the chat session
            services.AddSingleton<TAgent>(BuildAgent);
            // register the agent as a bootstrap for bootstrapping
            services.AddSingleton<IBootstrap>(serviceProvider => serviceProvider.GetRequiredService<TAgent>());
        }

        private McpClient BuildMcpClient(IServiceProvider serviceProvider)
        {
            return McpClient.CreateAsync(BuildClientTransport(serviceProvider),
                                               serviceProvider.GetRequiredService<IOptions<McpClientOptions>>().Value,
                                               serviceProvider.GetRequiredService<ILoggerFactory>())
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
        }

        private ChatOptions CreateChatOptions(IServiceProvider serviceProvider)
        {
            var options = new ChatOptions();
            var tools = new List<McpClientTool>();
            foreach (var mcpClient in serviceProvider.GetRequiredService<IEnumerable<McpClient>>())
            {
                tools.AddRange(mcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult());
            }
            options.Tools = [.. tools];
            return options;
        }

        protected abstract TAgent BuildAgent(IServiceProvider serviceProvider);

        protected abstract IClientTransport BuildClientTransport(IServiceProvider serviceProvider);

        private IChatClient BuildChatClient(IServiceProvider serviceProvider)
        {
            return new ChatClientBuilder(BuildInnerClient).UseFunctionInvocation(serviceProvider.GetRequiredService<ILoggerFactory>(),
                                                                                 functionInvokingChatClient => ConfigureFunctionInvokingChatClient(functionInvokingChatClient, serviceProvider))
                                                          .Build();
        }

        protected abstract IChatClient BuildInnerClient(IServiceProvider serviceProvider);

        private void ConfigureFunctionInvokingChatClient(FunctionInvokingChatClient functionInvokingChatClient, IServiceProvider serviceProvider)
        {
            // TODO     
        }
    }
}
