using Baubit.Configuration;
using Baubit.DI;
using Baubit.Testing.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Testing
{
    public class ScenarioBuilder<TContext> : IDisposable where TContext: IContext
    {
        private ConfigurationSourceBuilder _configurationSourceBuilder;
        private bool _isDisposed;

        private ScenarioBuilder(ConfigurationSourceBuilder configurationBuilder)
        {
            _configurationSourceBuilder = configurationBuilder;
        }

        public static Result<ScenarioBuilder<TContext>> CreateFromEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return ConfigurationSourceBuilder.CreateNew()
                                      .Bind(configSourceBuilder => Result.Try(() => new ScenarioBuilder<TContext>(configSourceBuilder))
                                                                         .Bind(scenarioBuilder => scenarioBuilder._configurationSourceBuilder.WithEmbeddedJsonResources(embeddedJsonResources)
                                                                                                                                             .Bind(_ => Result.Ok(scenarioBuilder))));
        }

        public Result<IScenario<TContext>> Build()
        {
            return _configurationSourceBuilder.Build().Bind(configSource => configSource.Build()).Bind(configuration => configuration.TryAs<IScenario<TContext>>());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _configurationSourceBuilder.Dispose();
                    _configurationSourceBuilder = null;
                }
                _isDisposed = true;
            }
        }
    }
}
