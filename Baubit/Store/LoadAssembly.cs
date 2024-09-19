using FluentResults;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<Assembly>> LoadAssemblyAsync(AssemblyLoadingContext context)
        {
            return await Result.Try((Func<Task<Assembly>>)(async () =>
            {
                await Task.Yield();
                foreach (var dep in context.Package.Dependencies)
                {
                    context.AssemblyLoadContext.LoadFromAssemblyPath(dep.DllFile);
                }
                return context.AssemblyLoadContext.LoadFromAssemblyPath(context.Package.DllFile);
            }));
        }
    }

    public class AssemblyLoadingContext
    {
        public Package Package { get; init; }
        public AssemblyLoadContext AssemblyLoadContext { get; init; }
        public AssemblyLoadingContext(Package package, AssemblyLoadContext assemblyLoadContext)
        {
            Package = package;
            AssemblyLoadContext = assemblyLoadContext;
        }

    }
}
