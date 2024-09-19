using FluentResults;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<Type>> ResolveTypeAsync(TypeResolutionContext context)
        {
            try
            {
                await Task.Yield();
                var type = Type.GetType(context.AssemblyQualifiedName, ResolveAssembly, (assembly, aqn, ignoreCase) => assembly.GetType(aqn, false, ignoreCase));
                if (type != null)
                {
                    return Result.Ok(type);
                }
                else
                {
                    return Result.Fail("");
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            Package package = null;
            var searchResult = Store.Operations
                                    .SearchPackageAsync(new Store.PackageSearchContext(Application.BaubitPackageRegistry, assemblyName, Application.TargetFramework))
                                    .GetAwaiter()
                                    .GetResult();
            if (searchResult.IsSuccess)
            {
                package = searchResult.Value;
            }
            else
            {
                var downloadResult = Store.Operations
                                          .DownloadPackageAsync(new PackageDownloadContext(assemblyName, Application.TargetFramework, Application.BaubitRootPath, true))
                                          .GetAwaiter()
                                          .GetResult();

                if (downloadResult.IsSuccess)
                {
                    package = downloadResult.Value;
                }
            }
            if (package == null)
            {
                return null;
            }
            var loadResult = Store.Operations.LoadAssemblyAsync(new AssemblyLoadingContext(package, AssemblyLoadContext.Default)).GetAwaiter().GetResult();
            if (loadResult.IsSuccess)
            {
                return loadResult.Value;
            }
            else
            {
                return null;
            }
        }
    }
    public class TypeResolutionContext
    {
        public string AssemblyQualifiedName { get; init; }
        public TypeResolutionContext(string assemblyQualifiedName)
        {
            AssemblyQualifiedName = assemblyQualifiedName;
        }
    }
}
