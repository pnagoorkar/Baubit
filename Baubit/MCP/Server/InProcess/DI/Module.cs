using Baubit.Bootstrapping;
using Baubit.Configuration;
using Baubit.MCP.Server.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Net;
using System.Net.Sockets;

namespace Baubit.MCP.Server.InProcess.DI
{
    public class Module<TServer> : AModule<Configuration>
    {
        //Stream? serverSideStream = null;
        //Stream? clientSideStream = null;

        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<Baubit.DI.IModule> nestedModules, List<Baubit.DI.IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }


        //protected override void OnInitialized()
        //{
        //    CreateLoopbackStreams().Wait();
        //}

        //private async Task CreateLoopbackStreams()
        //{
        //    var listener = new TcpListener(IPAddress.Loopback, 0);
        //    listener.Start();
        //    var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        //    var client = new TcpClient();
        //    var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
        //    var clientProxy = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
        //    await connectTask.ConfigureAwait(false);
        //    listener.Stop();

        //    serverSideStream = clientProxy.GetStream();
        //    clientSideStream = client.GetStream();
        //}

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

        //protected override ITransport BuildServerTransport(IServiceProvider serviceProvider)
        //{
        //    return new StreamServerTransport(serverSideStream, serverSideStream, Configuration.ServerName, serviceProvider.GetRequiredService<ILoggerFactory>());
        //}

        //private async Task<McpClient> BuildClientAsync(IServiceProvider serviceProvider)
        //{
        //    return await McpClient.CreateAsync(BuildStreamClientTransport(serviceProvider), 
        //                                       serviceProvider.GetRequiredService<IOptions<McpClientOptions>>().Value, 
        //                                       serviceProvider.GetRequiredService<ILoggerFactory>())
        //                           .ConfigureAwait(false);
        //}

        //private StreamClientTransport BuildStreamClientTransport(IServiceProvider serviceProvider)
        //{
        //    return new StreamClientTransport(clientSideStream, clientSideStream, serviceProvider.GetRequiredService<ILoggerFactory>());
        //}
    }
}
