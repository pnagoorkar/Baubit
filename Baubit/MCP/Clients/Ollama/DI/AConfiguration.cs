using Baubit.MCP.Clients.DI;
using OllamaSharp;

namespace Baubit.MCP.Clients.Ollama.DI
{
    public abstract record AConfiguration : Clients.DI.AConfiguration
    {
        public OllamaApiClient.Configuration OllamaApiClientConfig { get; init; }
    }
}
