using Baubit.Operation;
using System.Text.Json;

namespace Baubit.Store
{
    public sealed class AddToRegistry : IOperation<AddToRegistry.Context, AddToRegistry.Result>
    {
        private AddToRegistry()
        {

        }
        private static AddToRegistry _singletonInstance = new AddToRegistry();
        public static AddToRegistry GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                await Task.Yield();
                Application.BaubitStoreRegistryAccessor.WaitOne();
                PackageRegistry registry = null;
                if (File.Exists(context.RegistryFilePath))
                {
                    var registryContents = File.ReadAllText(context.RegistryFilePath);
                    registry = JsonSerializer.Deserialize<PackageRegistry>(registryContents, Application.IndentedJsonWithCamelCase);
                }
                else
                {
                    registry = new PackageRegistry();
                }

                var packages = new List<Package>();
                context.Package.TryFlatteningPackage(packages);

                if (registry.ContainsKey(context.TargetFramework))
                {
                    foreach(var p in packages)
                    {
                        if(registry[context.TargetFramework].Any(package => package.AssemblyName.Name.Equals(context.Package.AssemblyName.Name, 
                                                                                                             StringComparison.OrdinalIgnoreCase) &&
                                                                             package.AssemblyName.Version.Equals(context.Package.AssemblyName.Version)))
                        {
                            //Do nothing. The package already exists in the registry !
                        }
                        else
                        {
                            registry[context.TargetFramework].Add(p);
                        }
                    }
                }
                else
                {
                    registry.Add(context.TargetFramework, packages);
                }
                File.WriteAllText(context.RegistryFilePath, JsonSerializer.Serialize(registry, Application.IndentedJsonWithCamelCase));
                return new Result(true, true);
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
            finally { Application.BaubitStoreRegistryAccessor.ReleaseMutex(); }
        }

        public sealed class Context : IContext
        {
            public string RegistryFilePath { get; init; }
            public Package Package { get; init; }
            public string TargetFramework { get; init; }
            public Context(string registryFilePath, Package package, string targetFramework)
            {
                RegistryFilePath = registryFilePath;
                Package = package;
                TargetFramework = targetFramework;
            }
        }

        public sealed class Result : AResult<bool>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, bool? value) : base(success, value.Value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
