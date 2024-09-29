using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;

namespace Baubit.Store
{
    public class PackageRegistry : Dictionary<string, List<Package>>
    {
        static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        private PackageRegistry(IEnumerable<KeyValuePair<string, List<Package>>> collection) : base(collection)
        {

        }

        private static Result<PackageRegistry> Read(string jsonFileSource)
        {
            try
            {
                if (!File.Exists(jsonFileSource)) return Result.Ok(new PackageRegistry(Enumerable.Empty<KeyValuePair<string, List<Package>>>()));

                var source = new MetaConfiguration { JsonUriStrings = [jsonFileSource] };
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
        }

        public static Result<Package> Search(string jsonFileSource, string targetFramework, AssemblyName assemblyName)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                var readResult = Read(jsonFileSource);
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
            finally
            {
                BaubitStoreRegistryAccessor.ReleaseMutex();
            }
        }

        public static Result Add(string jsonFileSource, IEnumerable<Package> packages, string targetFramework)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                var readResult = Read(jsonFileSource);
                if (!readResult.IsSuccess)
                {
                    return Result.Fail("").WithReasons(readResult.Reasons);
                }

                if (!readResult.Value.ContainsKey(targetFramework))
                {
                    readResult.Value.Add(targetFramework, new List<Package>());
                }

                 var addablePackages = packages.SelectMany(package => package.GetAllTrees())
                                               .Where(package => readResult.Value[targetFramework]
                                                                           .Search(package.AssemblyName) == null);
                if (addablePackages.Any())
                {
                    readResult.Value[targetFramework].AddRange(addablePackages);
                }
                                               
                return readResult.Value.Persist(jsonFileSource);
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

        private Result Persist(string jsonFileSource)
        {
            try
            {
                foreach (var key in Keys)
                {
                    this[key] = this[key].DistinctBy(package => package.AssemblyName.GetPersistableAssemblyName())
                                         .OrderBy(package => package.AssemblyName.Name)
                                         .ThenBy(package => package.AssemblyName.Version)
                                         .ThenBy(package => package.Dependencies)
                                         .ToList();
                }
                File.WriteAllText(jsonFileSource, JsonSerializer.Serialize(this, Application.IndentedJsonWithCamelCase));
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
