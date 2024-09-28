using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Baubit.Store
{
    public class MockProject
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string PackageDeterminationWorkspace { get; init; }
        public string TempProjFileName { get; init; }
        public string TempProjBuildOutputFolder { get; init; }
        public string ProjectAssetsJsonFile { get; init; }

        public MockProject(AssemblyName assemblyName, string targetFramework)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            PackageDeterminationWorkspace = Path.Combine(Path.GetTempPath(), $"BaubitWorkspace_{AssemblyName.Name}");
            TempProjFileName = Path.Combine(PackageDeterminationWorkspace, $"BaubitMockConsumer_{AssemblyName.Name}.csproj");
            TempProjBuildOutputFolder = Path.Combine(PackageDeterminationWorkspace, "release");
            ProjectAssetsJsonFile = Path.Combine(PackageDeterminationWorkspace, "obj", $@"project.assets.json");
        }

        public async Task<Result<Package2>> BuildAsync()
        {
            return await GenerateProjectFile().Bind(BuildProject)
                                              .Bind(ReadProjectAssetsFile)
                                              .Bind(projectAssets => projectAssets.BuildPackage(AssemblyName));
        }

        private async Task<Result> GenerateProjectFile()
        {
            return await Result.Try(Assembly.GetExecutingAssembly)
                               .Bind(assembly => assembly.ReadResource($"{Assembly.GetExecutingAssembly().GetName().Name}.Store.CSProjTemplate.txt"))
                               .Bind(contents => Result.Try(() => contents.Replace("<TARGET_FRAMEWORK>", TargetFramework)
                                                                          .Replace("<PACKAGE_NAME>", AssemblyName.Name)
                                                                          .Replace("<PACKAGE_VERSION>", AssemblyName.Version!.ToString())))
                               .Bind(contents => Result.Try(() => File.WriteAllText(TempProjFileName, contents)));
        }

        private async Task<Result> BuildProject()
        {
            return await Result.Try(() => new CSProjBuilder(TempProjFileName, TempProjBuildOutputFolder))
                               .Bind(csProjBuilder => csProjBuilder.RunAsync());
        }

        private async Task<Result<ProjectAssets>> ReadProjectAssetsFile()
        {
            try
            {
                await Task.Yield();
                var targetFrameworkTargets = new ConfigurationBuilder().AddJsonFile(ProjectAssetsJsonFile)
                                                                       .Build()
                                                                       .GetSection("targets")
                                                                       .GetChildren()
                                                                       .FirstOrDefault(child => child.Key.Equals(TargetFramework));

                if (targetFrameworkTargets == null) return Result.Fail($"Failed to read {ProjectAssetsJsonFile}");
                if (!targetFrameworkTargets.Exists()) return Result.Fail($"Failed to read {ProjectAssetsJsonFile}");

                var projectAssets = new ProjectAssets(targetFrameworkTargets);
                return Result.Ok(projectAssets);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public static class ProcessExtensions
    {
        public static async Task<Result<string>> RunAsync(this ProcessStartInfo processStartInfo)
        {
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process == null) return Result.Fail(new ProcessFailedToStart());

                    await process.WaitForExitAsync();

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0) return Result.Ok(output).WithReason(new ProcessExitedWithZeroReturn(output));
                    else return Result.Fail(error).WithReason(new ProcessExitedWithNonZeroReturn(process.ExitCode, error));
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class ProcessExitedWithZeroReturn : IReason
    {
        public string Message => "";

        public Dictionary<string, object> Metadata { get; }

        public string Output { get; init; }

        public ProcessExitedWithZeroReturn(string output)
        {
            Output = output;
        }
    }


    public class ProcessExitedWithNonZeroReturn : IReason
    {
        public string Message => "";

        public Dictionary<string, object> Metadata { get; }

        public int ReturnCode { get; init; }
        public string Error { get; init; }

        public ProcessExitedWithNonZeroReturn(int returnCode, string error)
        {
            ReturnCode = returnCode;
            Error = error;
        }
    }

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
        public IConfigurationSection PackageConfigurationSection { get; init; }
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
            DllRelativePath = PackageConfigurationSection!.GetSection("runtime")?
                                                          .GetChildren()
                                                          .FirstOrDefault()?
                                                          .Key!;

            Dependencies = PackageConfigurationSection!.GetSection("dependencies")?
                                                       .GetChildren()
                                                       .Select(childSection => $"{childSection.Key}/{childSection.Value}")
                                                       .Select(packageKey => new ProjectAssetsPackage(targetFrameworkTargets!.GetChildren()
                                                                                                                             .FirstOrDefault(child => child.Key.StartsWith(packageKey!, StringComparison.OrdinalIgnoreCase))!, targetFrameworkTargets))!;
        }
    }
}
