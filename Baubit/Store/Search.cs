using System.Reflection;
using FluentResults;

namespace Baubit.Store
{

    public static partial class Operations
    {
        public static async Task<Result<SearchResult>> SearchPackageAsync(PackageSearchContext context)
        {
            try
            {
                await Task.Yield();
                return PackageRegistry.ReadFrom(context.RegistryFilePath)
                                      .Bind(registry => FindPackageInRegistry(registry, context));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static Result<SearchResult> FindPackageInRegistry(PackageRegistry packageRegistry, PackageSearchContext context)
        {
            try
            {
                var package = packageRegistry![context.TargetFramework].FirstOrDefault(package => package.AssemblyName.Name!.Equals(context.AssemblyName.Name, StringComparison.OrdinalIgnoreCase) &&
                                                                                                  package.AssemblyName.Version!.Equals(context.AssemblyName.Version));
                if (package == null)
                {
                    return Result.Fail("Package not found !");
                }
                else
                {
                    return Result.Ok(new SearchResult(packageRegistry, package));
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class PackageSearchContext
    {
        public string RegistryFilePath { get; init; }
        public AssemblyName AssemblyName { get; set; }
        public string TargetFramework { get; init; }
        public PackageSearchContext(string registryFilePath, AssemblyName assemblyName, string targetFramework)
        {
            RegistryFilePath = registryFilePath;
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
        }
    }

    public class SearchResult
    {
        public PackageRegistry Registry { get; init; }
        public Package Package { get; init; }
        public SearchResult(PackageRegistry registry, Package package)
        {
            this.Registry = registry;
            this.Package = package;
        }
    }
}
