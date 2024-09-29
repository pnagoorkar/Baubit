using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Baubit.Store
{
    public class PackageRegistry : Dictionary<string, List<Package>>
    {
        static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        public PackageRegistry(IEnumerable<KeyValuePair<string, List<Package>>> collection) : base(collection)
        {

        }

        public static Result<PackageRegistry> Read(MetaConfiguration source)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();

                var registry = new PackageRegistry(source.Load()
                                                         .GetChildren()
                                                         .Select(targetFrameworkSection => new KeyValuePair<string, List<Package>>(targetFrameworkSection.Key, 
                                                                                                                                   targetFrameworkSection.GetChildren()
                                                                                                                                                         .Select(packageSection => packageSection.Get<SerializablePackage>())!
                                                                                                                                                         .AsPackages())));

                return Result.Ok(registry);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            finally
            {
                BaubitStoreRegistryAccessor.ReleaseMutex();
            }
        }

        public static Result<Package> Search(MetaConfiguration source, string targetFramework, AssemblyName assemblyName)
        {
            try
            {
                var readResult = Read(source);
                if (!readResult.IsSuccess)
                {
                    return Result.Fail("").WithReasons(readResult.Reasons);
                }

                if (!readResult.Value.ContainsKey(targetFramework))
                {
                    return Result.Fail($"Registry not defined for {targetFramework}");
                }

                var package = readResult.Value[targetFramework].Search(assemblyName);

                if (package == null)
                {
                    return Result.Fail("Package not found !");
                }

                return Result.Ok(package);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
