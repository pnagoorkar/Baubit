using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Configuration;
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
            var currentAssembly = Assembly.GetExecutingAssembly();
            var resourceFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.Store.CSProjTemplate.txt";
            return await currentAssembly.ReadResource(resourceFile)
                                        .Bind(contents => Result.Try(() => contents.Replace("<TARGET_FRAMEWORK>", TargetFramework)
                                                                                   .Replace("<PACKAGE_NAME>", AssemblyName.Name)
                                                                                   .Replace("<PACKAGE_VERSION>", AssemblyName.Version!.ToString())))
                                        .Bind(WriteCSProjectToFile);
        }

        private async Task<Result> WriteCSProjectToFile(string contents)
        {
            await Task.Yield();
            if (Directory.Exists(PackageDeterminationWorkspace)) Directory.Delete(PackageDeterminationWorkspace, true);

            Directory.CreateDirectory(PackageDeterminationWorkspace); 

            File.WriteAllText(TempProjFileName, contents);

            return Result.Ok();
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
}
