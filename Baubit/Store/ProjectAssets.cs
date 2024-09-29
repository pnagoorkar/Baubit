using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Store
{
    public class ProjectAssets
    {
        public IConfiguration Configuration { get; init; }
        private ProjectAssets(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public static Result<ProjectAssets> Read(MetaConfiguration source)
        {
            return Result.Try(() => new ProjectAssets(source.Load()));
        }
    }

    public static class ProjectAssetsExtensions
    {
        public static IConfigurationSection GetTargetFrameworkTargets(this ProjectAssets projectAssets, string targetFramework)
        {
            return projectAssets.Configuration
                                .GetSection("targets")
                                .GetChildren()
                                .FirstOrDefault(child => child.Key.Equals(targetFramework))!;
        }

        public static Package BuildPackage(this ProjectAssets projectAssets, string assemblyName, string targetFramework)
        {
            return projectAssets.BuildSerializablePackages(assemblyName, targetFramework)
                                .AsPackages()
                                .FirstOrDefault(package => package.Equals(assemblyName))!;
        }

        public static IEnumerable<SerializablePackage> BuildSerializablePackages(this ProjectAssets projectAssets, string assemblyName, string targetFramework)
        {
            var assemblyConfigurationSection = projectAssets.GetTargetFrameworkTargets(targetFramework)!
                                                            .GetChildren()
                                                            .FirstOrDefault(child => child.Key.StartsWith(assemblyName!, StringComparison.OrdinalIgnoreCase));
            var serializablePackage = new SerializablePackage
            {
                AssemblyName = assemblyConfigurationSection.Key,
                DllRelativePath = assemblyConfigurationSection!.GetSection("runtime")?
                                                               .GetChildren()
                                                               .FirstOrDefault()?
                                                               .Key!,
                Dependencies = assemblyConfigurationSection!.GetSection("dependencies")?.GetChildren().Select(section => $"{section.Key}/{section.Value}").ToList()!
            };
            yield return serializablePackage;
            foreach (var dependency in serializablePackage.Dependencies.SelectMany(depString => projectAssets.BuildSerializablePackages(depString, targetFramework)))
            {
                yield return dependency;
            }
        }
    }
}
