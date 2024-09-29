using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Baubit.Store
{
    public class ProjectAssets
    {
        public IConfigurationSection TargetFrameworkTargets { get; init; }
        public ProjectAssets(IConfigurationSection targetFrameworkTargets)
        {
            TargetFrameworkTargets = targetFrameworkTargets;
        }

        public async Task<Result<Package2>> BuildPackage(AssemblyName assemblyName)
        {
            try
            {
                var assemblyConfigurationSection = TargetFrameworkTargets!.GetChildren()
                                                                          .FirstOrDefault(child => child.Key.StartsWith(assemblyName.GetPersistableAssemblyName()!, StringComparison.OrdinalIgnoreCase));
                ProjectAssetsPackage projectAssetsPackage = new ProjectAssetsPackage(assemblyConfigurationSection!, TargetFrameworkTargets);

                return Result.Ok(Package2.BuildPackage(projectAssetsPackage).Value);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class ProjectAssetsPackage
    {
        //public IConfigurationSection PackageConfigurationSection { get; init; }
        public string AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        public IEnumerable<ProjectAssetsPackage> Dependencies { get; init; }

        private IConfigurationSection _packageConfigurationSection;
        private IConfigurationSection _targetFrameworkTargets;

        public ProjectAssetsPackage(IConfigurationSection packageConfigurationSection, IConfigurationSection targetFrameworkTargets)
        {
            _packageConfigurationSection = packageConfigurationSection;
            _targetFrameworkTargets = targetFrameworkTargets;

            AssemblyName = packageConfigurationSection.Key;
            DllRelativePath = _packageConfigurationSection!.GetSection("runtime")?
                                                          .GetChildren()
                                                          .FirstOrDefault()?
                                                          .Key!;

            Dependencies = _packageConfigurationSection!.GetSection("dependencies")?
                                                       .GetChildren()
                                                       .Select(childSection => $"{childSection.Key}/{childSection.Value}")
                                                       .Select(packageKey => new ProjectAssetsPackage(targetFrameworkTargets!.GetChildren()
                                                                                                                             .FirstOrDefault(child => child.Key.StartsWith(packageKey!, StringComparison.OrdinalIgnoreCase))!, targetFrameworkTargets))!;
        }
    }
}
