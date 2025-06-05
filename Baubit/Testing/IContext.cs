using FluentResults;

namespace Baubit.Testing
{
    public interface IContext
    {
    }


    public static class ContextExtensions
    {
        public static Result<TScenario> In<TScenario, TContext>(this TContext context,
                                                                params string[] embeddedJsonResources) where TScenario : class, IScenario<TContext> where TContext : IContext
        {
            return ScenarioBuilder<TScenario, TContext>.Create()
                                                       .Bind(bldr => bldr.WithEmbeddedJsonResources(embeddedJsonResources))
                                                       .Bind(bldr => bldr.Build());
        }
        public static Result TestIn<TScenario, TContext>(this TContext context,
                                                                  params string[] embeddedJsonResources) where TScenario : class, IScenario<TContext> where TContext : IContext
        {
            return context.In<TScenario, TContext>(embeddedJsonResources)
                          .Bind(scenario => scenario.Run(context));
        }
        public static async Task<Result> RunInAsync<TScenario, TContext>(this TContext context,
                                                                                   params string[] embeddedJsonResources) where TScenario : class, IScenario<TContext> where TContext : IContext
        {
            return await context.In<TScenario, TContext>(embeddedJsonResources)
                                .Bind(scenario => scenario.RunAsync(context));
        }
    }
}
