using FluentResults;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<Assembly>> LoadAssemblyAsync(AssemblyLoadingContext context)
        {
            try
            {
                foreach (var dep in context.Dependencies)
                {
                    var depLoadResult = await LoadAssemblyAsync(new AssemblyLoadingContext(dep, context.Registry, context.TargetFramework, context.AssemblyLoadContext));
                    if (!depLoadResult.IsSuccess)
                    {
                        return Result.Fail("").WithReasons(depLoadResult.Reasons);
                    }
                }
                return context.AssemblyLoadContext.LoadFromAssemblyPath(context.Package.DllFile);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class AssemblyLoadingContext
    {
        public Package Package { get; init; }
        public IEnumerable<Package> Dependencies { get => Registry[TargetFramework].Where(package => Package.Dependencies
                                                                                                            .Any(p => p.Equals(package.AssemblyName.GetPersistableAssemblyName(), 
                                                                                                                      StringComparison.OrdinalIgnoreCase))); }
        public PackageRegistry Registry { get; init; }
        public string TargetFramework { get; init; }
        public AssemblyLoadContext AssemblyLoadContext { get; init; }
        public AssemblyLoadingContext(Package package, PackageRegistry registry, string targetFramework, AssemblyLoadContext assemblyLoadContext)
        {
            Package = package;
            Registry = registry;
            TargetFramework = targetFramework;
            AssemblyLoadContext = assemblyLoadContext;
        }

    }
}
