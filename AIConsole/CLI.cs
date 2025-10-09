using Baubit.MCP;
using Baubit.Bootstrapping;
using Baubit.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace AIConsole
{
    public class CLI : IBootstrap
    {
        private IHub _hub;
        private CancellationTokenSource runnerCTS = new CancellationTokenSource();
        private Task<bool> runner;
        private bool disposedValue;

        public CLI(IHub hub)
        {
            _hub = hub;
            runner = RunAsync(runnerCTS.Token);
        }

        private async Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Begin chat..");
            var messages = new List<ChatMessage>();
            while (!cancellationToken.IsCancellationRequested)
            {
                messages.Add(new ChatMessage(ChatRole.User, Console.ReadLine()));

                var response = await _hub.PublishAsyncAsync<AgentRequest, AgentResponse>(new AgentRequest { Messages = messages }, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(response.Messages.Last().Text);
            }
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    runnerCTS.Cancel();
                    runner.Wait();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
