using Baubit.Configuration;
using Baubit.MCP.Server.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.MCP.Server.InProcess.DI
{
    public class Module<TServer> : AModule<Configuration>
    {

        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<Baubit.DI.IModule> nestedModules, List<Baubit.DI.IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton<McpServer>();
            services.AddMcpServer().WithTools<TServer>();
            base.Load(services);
        }

        protected override ModelContextProtocol.Server.McpServer BuildMcpServer(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<McpServer>().InternalServer;
        }
    }
}
