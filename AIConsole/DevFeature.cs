using Baubit.Configuration;
using Baubit.DI;
using OllamaInProcessClientModule = Baubit.MCP.Clients.Ollama.InProcess.DI.Module;
using OllamaInProcessClientModuleConfiguration = Baubit.MCP.Clients.Ollama.InProcess.DI.Configuration;
using DebugLoggingFeature = Baubit.Logging.Features.F001;
using EventsModule = Baubit.Events.DI.Module;
using InMemoryCacheModule = Baubit.Caching.InMemory.DI.Module<object>;
using InMemoryCacheModuleConfiguration = Baubit.Caching.InMemory.DI.Configuration;
using InProcessServerModule = Baubit.MCP.Server.InProcess.DI.Module<AIConsole.Server>;
using InProcessServerModuleConfiguration = Baubit.MCP.Server.InProcess.DI.Configuration;
using BootstrapperModule = Baubit.Bootstrapping.DI.Module<Baubit.Bootstrapping.Bootstrapper>;

namespace AIConsole
{
    public class DevFeature : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            ..new DebugLoggingFeature().Modules,
            new InMemoryCacheModule(InMemoryCacheModuleConfiguration, [],[]),
            new InProcessServerModule(InProcessServerModuleConfiguration, [], []), // Always add server before client otherwise the client bootstrapper WILL get stuck (as the server is not yet bootstrapped)
            new OllamaInProcessClientModule(AIAgentModuleConfiguration, [], []),
            new EventsModule(ConfigurationSource.Empty),
            new DI.Module(ConfigurationSource.Empty), // include the CLI as a bootstrap
            new BootstrapperModule(ConfigurationSource.Empty)
        ];

        public InMemoryCacheModuleConfiguration InMemoryCacheModuleConfiguration { get; init; }
        public OllamaInProcessClientModuleConfiguration AIAgentModuleConfiguration { get; init; }
        public InProcessServerModuleConfiguration InProcessServerModuleConfiguration { get; init; }

        public DevFeature()
        {
            InMemoryCacheModuleConfiguration = new InMemoryCacheModuleConfiguration
            {
                CacheConfiguration = new Baubit.Caching.Configuration
                {
                    EvictAfterEveryX = 1
                }
            };
            AIAgentModuleConfiguration = new OllamaInProcessClientModuleConfiguration
            {
                OllamaApiClientConfig = new OllamaSharp.OllamaApiClient.Configuration
                {
                    Uri = new Uri("http://localhost:11434"),
                    Model = "gpt-oss:20b"
                }
            };
            InProcessServerModuleConfiguration = new InProcessServerModuleConfiguration
            {

            };
        }
    }
}
