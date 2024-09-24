using FluentResults;
using FluentResults.Extensions;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Store
{
    public class TypeResolver
    {
        private TypeResolver()
        {
            
        }
        public static async Task<Result<Type>> ResolveTypeAsync(string assemblyQualifiedName)
        {
            return await new TypeResolver().ResolveAsync(assemblyQualifiedName);
        }

        private async Task<Result<Type>> ResolveAsync(string assemblyQualifiedName)
        {
            try
            {
                await Task.Yield();
                var type = Type.GetType(assemblyQualifiedName, ResolveAssembly, ResolveType)!;
                if (type == null)
                {
                    return Result.Fail("").WithReasons(searchAndLoadResult.Reasons);
                }
                return Result.Ok(type);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        Result<Assembly> searchAndLoadResult = null;
        private Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            try
            {
                searchAndLoadResult = SearchDownloadAndLoadAssembly(assemblyName).GetAwaiter().GetResult();

                return searchAndLoadResult.IsSuccess ? searchAndLoadResult.Value : null;
            }
            catch (Exception exp)
            {
                searchAndLoadResult = Result.Fail(new ExceptionalError(exp));
                return null;
            }
        }

        private Type? ResolveType(Assembly? assembly, string assemblyQualifiedName, bool ignoreCase) => assembly?.GetType(assemblyQualifiedName, ignoreCase);

        private static async Task<Result<Assembly>> SearchDownloadAndLoadAssembly(AssemblyName assemblyName)
        {
            try
            {
                var searchRes = await PackageRegistry.SearchAsync(assemblyName, Application.TargetFramework);
                if (!searchRes.IsSuccess)
                {
                    searchRes = await assemblyName.DetermineDownloadablePackagesAsync(Application.TargetFramework)
                                                  .Bind(package => package.DownloadAsync(true));
                }
                if (!searchRes.IsSuccess)
                {
                    return Result.Fail("").WithReasons(searchRes.Reasons);
                }
                return await searchRes.Value.Load(AssemblyLoadContext.Default);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
