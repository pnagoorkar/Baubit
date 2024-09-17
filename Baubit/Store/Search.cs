using System.Reflection;
using FluentResults;
using FluentResults.Extensions;

namespace Baubit.Store
{

    public static partial class Operations
    {
        public static async Task<Result<Package>> SearchPackageAsync(PackageSearchContext context)
        {
            try
            {
                Application.BaubitStoreRegistryAccessor.WaitOne();
                return await FileSystem.Operations
                                       .ReadFileAsync(new FileSystem.FileReadContext(context.RegistryFilePath))
                                       .Bind(jsonString => Serialization.Operations<PackageRegistry>.DeserializeJson(new Serialization.JsonDeserializationContext<PackageRegistry>(jsonString)))
                                       .Bind(registry => FindPackageInRegistry(registry, context));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            finally
            {
                Application.BaubitStoreRegistryAccessor.ReleaseMutex();
            }
        }

        private static Result<Package> FindPackageInRegistry(PackageRegistry packageRegistry, PackageSearchContext context)
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
                    return Result.Ok(package);
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
}
