using Baubit.Bootstrapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Baubit.MCP.Server.DI
{
    public abstract class AModule<TConfiguration> : Baubit.DI.AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        public AModule(Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public AModule(IConfiguration configuration) : base(configuration)
        {
        }

        public AModule(TConfiguration configuration, List<Baubit.DI.IModule> nestedModules, List<Baubit.DI.IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton(BuildMcpServer);
            services.AddSingleton<IBootstrap, ServerBootstrap>();
        }

        protected abstract McpServer BuildMcpServer(IServiceProvider serviceProvider);
    }

    public class ServerBootstrap : IBootstrap
    {
        CancellationTokenSource serverCTS = new CancellationTokenSource();
        Task serverRunner;
        private bool disposedValue;

        public ServerBootstrap(McpServer mcpServer)
        {
            serverRunner = mcpServer.RunAsync(serverCTS.Token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    serverCTS.Cancel();
                    serverRunner.Wait();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
