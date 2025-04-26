using Baubit.Configuration;
using Baubit.Reflection;
using Baubit.Testing;
using FluentResults;

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
                                                   .Bind(ObjectLoader.Load<Scenario>);

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
