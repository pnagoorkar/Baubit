using FluentResults;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Store
{
    public class TypeResolver
    {
        private TypeResolver()
        {
            
        }
        public static async Task<Result<Type>> ResolveTypeAsync(string assemblyQualifiedName, CancellationToken cancellationToken)
        {
            return await new TypeResolver().ResolveAsync(assemblyQualifiedName, cancellationToken);
        }

        private async Task<Result<Type>> ResolveAsync(string assemblyQualifiedName, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();

                var existingTypeResolutionResult = TryResolveFromExisting(assemblyQualifiedName);
                if(existingTypeResolutionResult.IsSuccess) return existingTypeResolutionResult.Value;

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

        private Result<Type> TryResolveFromExisting(string assemblyQualifiedName)
        {
            try
            {
                var type = Type.GetType(assemblyQualifiedName);
                if (type == null)
                {
                    return Result.Fail("");
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
                #region TryLoadingFromCurrentDomain
                //if (assemblyName.Version == null)
                //{
                //    var existingAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().IsSameAs(assemblyName));
                //    if (existingAssembly != null)
                //    {
                //        searchAndLoadResult = Result.Ok(existingAssembly);
                //        return existingAssembly;
                //    }
                //}
                #endregion

                if (assemblyName.Version == null)
                {
                    var versionDeterminationResult = assemblyName.DetermineAssemblyVersion().GetAwaiter().GetResult();
                    if (!versionDeterminationResult.IsSuccess)
                    {
                        searchAndLoadResult = Result.Fail("").WithReasons(versionDeterminationResult.Reasons);
                        return null;
                    }
                }

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
                Package loadablePackage = null;
                var searchRes = PackageRegistry.Search(Application.BaubitPackageRegistry, Application.TargetFramework, assemblyName);
                if (!searchRes.IsSuccess)
                {
                    var packageDeterminationResult = await assemblyName.DetermineDownloadablePackagesAsync(Application.TargetFramework);
                    if (!packageDeterminationResult.IsSuccess)
                    {
                        return Result.Fail("").WithReasons(searchRes.Reasons).WithReasons(packageDeterminationResult.Reasons);
                    }
                    loadablePackage = packageDeterminationResult.Value;
                    var downloadResult = await loadablePackage.DownloadAsync(true);
                    if (!downloadResult.IsSuccess)
                    {
                        return Result.Fail("").WithReasons(searchRes.Reasons).WithReasons(packageDeterminationResult.Reasons).WithReasons(downloadResult.Reasons);
                    }
                    var addResult = PackageRegistry.Add(Application.BaubitPackageRegistry, loadablePackage, Application.TargetFramework);
                    if (!addResult.IsSuccess)
                    {
                        return Result.Fail("").WithReasons(searchRes.Reasons).WithReasons(packageDeterminationResult.Reasons).WithReasons(downloadResult.Reasons).WithReasons(addResult.Reasons);
                    }
                }
                if (loadablePackage == null)
                {
                    return Result.Fail("");
                }
                return await loadablePackage.LoadAsync(AssemblyLoadContext.Default);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class AssemblyVersionConflict : IReason
    {
        public string Message => "The requested assembly is of a different version than assembly already loaded in context";

        public Dictionary<string, object> Metadata { get; } = default!;

        public string RequestedAssembly { get; init; }
        public string ExistingAssembly { get; init; }

        public AssemblyVersionConflict(AssemblyName requested, AssemblyName existing)
        {
            RequestedAssembly = requested.GetPersistableAssemblyName();
            ExistingAssembly = existing.GetPersistableAssemblyName();
        }
    }
}
