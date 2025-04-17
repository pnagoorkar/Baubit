using Baubit.Reflection;
using FluentResults;

namespace Baubit.Testing
{
    public interface IScenario : ISelfContained
    {
        Result Run();
        Task<Result> RunAsync();
    }
    public interface IScenario<TContext> : IScenario where TContext : IContext
    {
        Result Run(TContext context);
        Task<Result> RunAsync(TContext context);
    }
}
