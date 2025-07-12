
using Baubit.Testing;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Test.Testing.ScenarioBuilder.Setup
{
    public class Scenario : AScenario<Context, Scenario.Configuration>
    {
        public Scenario(IConfiguration configuration) : base(configuration)
        {
        }

        public override Result Run(Context context) => Result.Ok();

        public override Result Run() => Result.Ok();

        public override Task<Result> RunAsync(Context context) => Task.FromResult(Result.Ok());

        public override Task<Result> RunAsync() => Task.FromResult(Result.Ok());

        public class Configuration : Baubit.Testing.AConfiguration
        {

        }
    }
}
