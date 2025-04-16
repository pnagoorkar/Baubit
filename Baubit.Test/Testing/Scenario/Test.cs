using Baubit.Configuration;
using Baubit.Testing;
using FluentResults;

namespace Baubit.Test.Testing.Scenario
{
    public class Test
    {
        [Fact]
        public void CanLoadScenarioFromEmbeddedJsonResource()
        {
            var result = Result.Try(() => new ConfigurationSource<Scenario> { EmbeddedJsonResources = ["Baubit.Test;Testing.Scenario.scenario.json"] })
                               .Bind(configSource => configSource.Load<Scenario>());
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
    public class Context: IContext
    {

    }
    public class Scenario : IScenario<Context>
    {
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
