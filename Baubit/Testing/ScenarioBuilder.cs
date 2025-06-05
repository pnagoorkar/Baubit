using Baubit.Configuration;
using Baubit.DI;
using Baubit.Testing.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Testing
{
    public class ScenarioBuilder<TScenario, TContext> : IDisposable where TScenario :class, IScenario<TContext> where TContext : IContext
    {
        private ConfigurationSourceBuilder _configurationSourceBuilder;
        private bool _isDisposed;
        private ScenarioBuilder(ConfigurationSourceBuilder configurationBuilder)
        {
            _configurationSourceBuilder = configurationBuilder;
        }

        public static Result<ScenarioBuilder<TScenario, TContext>> Create()
        {
            return ConfigurationSourceBuilder.CreateNew().Bind(configSourceBuilder => Result.Try(() => new ScenarioBuilder<TScenario, TContext>(configSourceBuilder)));
        }

        public Result<ScenarioBuilder<TScenario, TContext>> WithEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return _configurationSourceBuilder.WithEmbeddedJsonResources(embeddedJsonResources).Bind(_ => Result.Ok(this));
        }

        public Result<TScenario> Build()
        {
            return FailIfDisposed().Bind(_configurationSourceBuilder.Build)
                                   .Bind(ComponentBuilder<TScenario>.Create)
                                   .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<TScenario>()))
                                   .Bind(compBuilder => compBuilder.Build());
        }

        private Result FailIfDisposed()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ScenarioBuilderDisposed());
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
