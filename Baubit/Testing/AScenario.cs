using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Testing
{
    public abstract class AScenario<TContext, TConfiguration> : IScenario<TContext> where TContext : IContext where TConfiguration : AConfiguration
    {
        public TConfiguration ScenarioConfiguration { get; init; }
        protected AScenario(IConfiguration configuration)
        {
            ScenarioConfiguration = configuration.Get<TConfiguration>()!;
        }
        public abstract Result Run(TContext context);
        public abstract Result Run();
        public abstract Task<Result> RunAsync(TContext context);
        public abstract Task<Result> RunAsync();
    }
}
