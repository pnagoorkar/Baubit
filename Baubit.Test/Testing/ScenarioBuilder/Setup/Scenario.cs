
using Baubit.Testing;
using FluentResults;

namespace Baubit.Test.Testing.ScenarioBuilder.Setup
{
    public class Scenario : IScenario<Context>
    {
        public Result Run(Context context) => Result.Ok();

        public Result Run() => Result.Ok();

        public Task<Result> RunAsync(Context context) => Task.FromResult(Result.Ok());

        public Task<Result> RunAsync() => Task.FromResult(Result.Ok());
    }
}
