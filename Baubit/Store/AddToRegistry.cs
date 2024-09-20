using FluentResults;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<PackageRegistry>> AddToRegistryAsync(List<Package> packages, RegistryAddContext context)
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

                if (registry.ContainsKey(context.TargetFramework))
                {
                    registry[context.TargetFramework].AddRange(packages);
                }
                else
                {
                    registry.Add(context.TargetFramework, packages);
                }
                return registry.WriteTo(context.RegistryFilePath).Bind(() => Result.Ok(registry));
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
