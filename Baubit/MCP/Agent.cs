using Baubit.Bootstrapping;
using Baubit.Events;
using Baubit.MCP.Clients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Baubit.MCP
{
    public sealed class Agent : IAgent
    {
        private IChatClient _chatClient;
        private IList<McpClientTool> _tools;
        private CancellationTokenSource hubCTS = new CancellationTokenSource();
        private Task<bool> _hubSubscription;
        private bool disposedValue;
        private ILogger<Agent> _logger;

        public Agent(IChatClient chatClient,
                     IList<McpClientTool> tools, 
                     IHub hub, 
                     ILoggerFactory loggerFactory)
        {
            _chatClient = chatClient;
            _tools = tools;
            _hubSubscription = hub.SubscribeAsync<AgentRequest, AgentResponse>(this, hubCTS.Token);
            _logger = loggerFactory.CreateLogger<Agent>();
        }

        public AgentResponse Handle(AgentRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<AgentResponse> HandleSyncAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AgentResponse> HandleAsyncAsync(AgentRequest request)
        {
            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in _chatClient.GetStreamingResponseAsync(request.Messages, new() { Tools = [.. _tools] }).ConfigureAwait(false))
            {
                updates.Add(update);
            }
            request.Messages.AddMessages(updates);
            return new AgentResponse { Messages = request.Messages };
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    hubCTS.Cancel();
                    _hubSubscription.Wait();
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

    public class AgentResponse : IResponse
    {
        public List<ChatMessage> Messages { get; init; }
    }

    public class AgentRequest : IRequest<AgentResponse>
    {
        public List<ChatMessage> Messages { get; init; }
    }
}
