using Baubit.Bootstrapping;
using Baubit.Events;
using Baubit.MCP.Clients;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace Baubit.MCP
{
    public sealed class Agent : IAgent
    {
        private IChatClient _chatClient;
        private ChatOptions _chatOptions;
        private CancellationTokenSource hubCTS = new CancellationTokenSource();
        private Task<bool> _hubSubscription;
        private bool disposedValue;
        private ILogger<Agent> _logger;

        public Agent(IChatClient chatClient,
                     ChatOptions chatOptions,
                     IHub hub,
                     ILoggerFactory loggerFactory)
        {
            _chatClient = chatClient;
            _chatOptions = chatOptions;
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
            await foreach (var update in _chatClient.GetStreamingResponseAsync(request.Messages, _chatOptions).ConfigureAwait(false))
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
        [IgnoreMember]
        public List<ChatMessage> Messages { get; init; }

        public List<string> JsonSerializedMessages
        {
            get => Messages.Select(msg => JsonSerializer.Serialize(msg)).ToList();
            init
            {
                Messages = value.Select(json => JsonSerializer.Deserialize<ChatMessage>(json)).ToList();
            }
        }
    }

    public class AgentRequest : IRequest<AgentResponse>
    {
        [IgnoreMember]
        public List<ChatMessage> Messages { get; init; }

        public List<string> JsonSerializedMessages
        {
            get => Messages.Select(msg => JsonSerializer.Serialize(msg)).ToList();
            init
            {
                Messages = value.Select(json => JsonSerializer.Deserialize<ChatMessage>(json)).ToList();
            }
        }
    }
}
