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
    }
}
