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

        private MockProject(AssemblyName assemblyName, string targetFramework)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            PackageDeterminationWorkspace = Path.Combine(Path.GetTempPath(), $"BaubitWorkspace_{AssemblyName.Name}");
            TempProjFileName = Path.Combine(PackageDeterminationWorkspace, $"BaubitMockConsumer_{AssemblyName.Name}.csproj");
            TempProjBuildOutputFolder = Path.Combine(PackageDeterminationWorkspace, "release");
            ProjectAssetsJsonFile = Path.Combine(PackageDeterminationWorkspace, "obj", $@"project.assets.json");
        }

        public static Result<MockProject> Build(AssemblyName assemblyName, string targetFramework)
        {
            return Result.Try(() => new MockProject(assemblyName, targetFramework));
        }

        public async Task<Result<Package>> BuildAsync()
        {
            return await GenerateProjectFile().Bind(BuildProject)
                                              .Bind(ReadProjectAssetsFile)
                                              .Bind(projectAssets => Result.Try(() => projectAssets.BuildPackage(AssemblyName.GetPersistableAssemblyName(), TargetFramework)));
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

                 return ProjectAssets.Read(new Configuration.ConfigurationSource { JsonUriStrings = [ProjectAssetsJsonFile] });
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
