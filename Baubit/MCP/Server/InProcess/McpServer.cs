using Baubit.Networking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;

namespace Baubit.MCP.Server.InProcess
{
    public class McpServer
    {
        public TCPLoopback TCPLoopback { get; private set; }
        public ModelContextProtocol.Server.McpServer InternalServer { get; private set; }

        public McpServer(IServiceProvider serviceProvider)
        {

            InitializeAsync(serviceProvider).Wait();
        }

        private async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            TCPLoopback = await TCPLoopback.CreateNewAsync(cancellationToken).ConfigureAwait(false);
            var mcpServerOptions = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var transport = new StreamServerTransport(TCPLoopback.ServerSideStream, TCPLoopback.ServerSideStream,
                                                      mcpServerOptions?.ServerInfo?.Name,
                                                      loggerFactory);
            InternalServer = ModelContextProtocol.Server.McpServer.Create(transport, mcpServerOptions, loggerFactory, serviceProvider);
        }
    }
}
