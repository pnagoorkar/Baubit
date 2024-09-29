using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Store
{
    public class ProjectAssets2
    {
        public IConfiguration Configuration { get; init; }
        public ProjectAssets2(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public static Result<ProjectAssets2> Read(MetaConfiguration source)
        {
            return Result.Try(() => new ProjectAssets2(source.Load()));
        }
    }

    public static class ProjectAssetsExtensions
    {
        public static IConfigurationSection GetTargetFrameworkTargets(this ProjectAssets2 projectAssets, string targetFramework)
        {
            return projectAssets.Configuration
                                .GetSection("targets")
                                .GetChildren()
                                .FirstOrDefault(child => child.Key.Equals(targetFramework))!;
        }
        public static IEnumerable<SerializablePackage> BuildSerializablePackage(this ProjectAssets2 projectAssets, string assemblyName, string targetFramework)
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
            foreach (var dependency in serializablePackage.Dependencies.SelectMany(depString => projectAssets.BuildSerializablePackage(depString, targetFramework)))
            {
                yield return dependency;
            }
        }
    }
}
