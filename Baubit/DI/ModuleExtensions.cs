using Baubit.Validation;
using FluentResults;

namespace Baubit.DI
{
    public static class ModuleExtensions
    {
        public static Result<List<IModule>> TryFlatten<TModule>(this TModule module) where TModule : IModule
        {
            return Result.Try(() => new List<IModule>())
                         .Bind(modules => module.TryFlatten(modules) ? Result.Ok(modules) : Result.Fail(""));
        }
        public static bool TryFlatten<TModule>(this TModule module, List<IModule> modules) where TModule : IModule
        {
            if (modules == null) modules = new List<IModule>();

            modules.Add(module);

            foreach (var nestedModule in module.NestedModules)
            {
                nestedModule.TryFlatten(modules);
            }

            return true;
        }

        public static Result CheckConstraints<TRootModule>(this TRootModule rootModule) where TRootModule : IRootModule
        {
            return rootModule.TryFlatten()
                             .Bind(modules => modules.Remove(rootModule) ? Result.Ok(modules) : Result.Fail(string.Empty))
                             .Bind(modules => modules.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.CheckConstraints(modules))));
        }

        public static Result CheckConstraints<TModule>(this TModule module, List<IModule> modules) where TModule : class, IModule
        {
            return module.TryValidate(module.Configuration.ModuleValidatorTypes, modules.Cast<IConstrainable>().ToList()).Bind(m => Result.Ok());
        }
    }
}
