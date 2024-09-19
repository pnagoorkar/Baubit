using FluentResults;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result> AddToRegistryAsync(RegistryAddContext context)
        {
            try
            {
                PackageRegistry registry = null;

                var readResult = PackageRegistry.ReadFrom(context.RegistryFilePath);

                if (readResult.IsSuccess)
                {
                    registry = readResult.Value;
                }
                else
                {
                    registry = new PackageRegistry();
                }

                var packages = new List<Package>();
                context.Package.TryFlatteningPackage(packages);

                if (registry.ContainsKey(context.TargetFramework))
                {
                    foreach (var p in packages)
                    {
                        if (registry[context.TargetFramework].Any(package => package.AssemblyName.Name.Equals(context.Package.AssemblyName.Name,
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

                return registry.WriteTo(context.RegistryFilePath);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }

        }
    }

    public class RegistryAddContext
    {
        public string RegistryFilePath { get; init; }
        public Package Package { get; init; }
        public string TargetFramework { get; init; }
        public RegistryAddContext(string registryFilePath, Package package, string targetFramework)
        {
            RegistryFilePath = registryFilePath;
            Package = package;
            TargetFramework = targetFramework;
        }
    }
}
