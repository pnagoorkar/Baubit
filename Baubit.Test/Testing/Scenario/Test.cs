using Baubit.Configuration;
using Baubit.DI;
using Baubit.Testing;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.Testing.Scenario
{
    public class Test
    {
        [Fact]
        public void CanLoadScenarioFromEmbeddedJsonResource()
        {
            var result = ConfigurationSourceBuilder.CreateNew()
                                                   .Bind(configSourceBuilder => configSourceBuilder.WithEmbeddedJsonResources("Baubit.Test;Testing.Scenario.scenario.json"))
                                                   .Bind(configSourceBuilder => configSourceBuilder.Build())
                                                   .Bind(configSource => ComponentBuilder<Scenario>.Create(configSource))
                                                   .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<Scenario>()))
                                                   .Bind(compBuilder => compBuilder.Build());
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
    public class Context : IContext
    {
        public void Dispose()
        {

        }
    }
    public class Scenario : IScenario<Context>
    {
        public void Dispose()
        {

        }

        public Result Run(Context context)
        {
            throw new NotImplementedException();
        }

        public Result Run()
        {
            throw new NotImplementedException();
        }

        public Task<Result> RunAsync(Context context)
        {
            throw new NotImplementedException();
        }

        public Task<Result> RunAsync()
        {
            throw new NotImplementedException();
        }
    }
}
