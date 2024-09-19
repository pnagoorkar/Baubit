using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using FluentResults;
using FluentResults.Extensions;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<Package>> DetermineAssemblyDependenciesAsync(AssemblyDependencyDeterminationContext context)
        {
            try
            {
                return await Resource.Operations.ReadEmbeddedResourceAsync(new Resource.EmbeddedResourceReadContext($"{Assembly.GetExecutingAssembly().GetName().Name}.Store.CSProjTemplate.txt",
                                                                                                                       Assembly.GetExecutingAssembly()))
                                                .Bind(template => BuildCSProjFile(template, context))
                                                .Bind(template => WriteCSProjectToFile(template, context))
                                                .Bind(() => BuildProject(context))
                                                .Bind(() => Configuration.Operations.LoadFromJsonFileAsync(new Configuration.ConfiguratonLoadContext(context.ProjectAssetsJsonFile)))
                                                .Bind(configuration => BuildPackageFromProjectAssetsConfiguration(configuration, context));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static Result<string> BuildCSProjFile(string template, AssemblyDependencyDeterminationContext context)
        {
            return Result.Try(() => template.Replace("<TARGET_FRAMEWORK>", context.TargetFramework)
                                            .Replace("<PACKAGE_NAME>", context.AssemblyName.Name)
                                            .Replace("<PACKAGE_VERSION>", context.AssemblyName.Version!.ToString()));
        }

        private static async Task<Result> WriteCSProjectToFile(string template, AssemblyDependencyDeterminationContext context)
        {
            return await FileSystem.Operations
                                   .DeleteDirectoryIfExistsAsync(new FileSystem.DirectoryDeleteContext(context.PackageDeterminationWorkspace, true))
                                   .Bind(() => FileSystem.Operations
                                                         .CreateDirectoryAsync(new FileSystem.DirectoryCreateContext(context.PackageDeterminationWorkspace)))
                                                         .Bind(() => Result.Try((Func<Task>)(async () => { await Task.Yield(); File.WriteAllText(context.TempProjFileName, template); })));
        }                          

        private static async Task<Result> BuildProject(AssemblyDependencyDeterminationContext context)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build {context.TempProjFileName} --configuration Debug --output {context.TempProjBuildOutputFolder}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                return await Process.Operations.RunProcessAsync(new Process.ProcessRunContext(startInfo, null, null));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static async Task<Result<Package>> BuildPackageFromProjectAssetsConfiguration(IConfiguration configuration, AssemblyDependencyDeterminationContext context)
        {
            try
            {
                var targetFrameworkTargets = configuration.GetSection("targets")
                                                          .GetChildren()
                                                          .FirstOrDefault(child => child.Key.Equals(context.TargetFramework));

                var rootAssemblyTarget = targetFrameworkTargets.GetChildren()
                                                               .FirstOrDefault(child => child.Key.StartsWith(context.AssemblyName.Name, StringComparison.OrdinalIgnoreCase));

                if (TryConvertingConfigSectionToPackage(rootAssemblyTarget, targetFrameworkTargets.GetChildren().ToArray(), out var package))
                {
                    return Result.Ok(package);
                }
                else
                {
                    return Result.Fail(new UnableToBuildPackageFromProjectAssetsJson());
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        public static bool TryConvertingConfigSectionToPackage(IConfigurationSection rootConfigurationSection,
                                                               IConfigurationSection[] targetFrameworkTargets,
                                                               out Package package)
        {
            var keyParts = rootConfigurationSection.Key.Split('/');

            var dllRelativePath = rootConfigurationSection.GetChildren()
                                                      .FirstOrDefault(child => child.Key.Equals("runtime"))?
                                                      .GetChildren()
                                                      .FirstOrDefault()?
                                                      .Key;

            List<Package> dependencies = new List<Package>();

            foreach (var deps in rootConfigurationSection.GetSection("dependencies").GetChildren())
            {
                var depConfigSection = targetFrameworkTargets.FirstOrDefault(child => child.Key.StartsWith($"{deps.Key}/", StringComparison.OrdinalIgnoreCase));
                if (TryConvertingConfigSectionToPackage(depConfigSection, targetFrameworkTargets, out var dependency))
                {
                    dependencies.Add(dependency);
                }
            }

            package = new Package(new AssemblyName { Name = keyParts[0], Version = new Version(keyParts[1]) },
                                  dllRelativePath,
                                  dependencies.ToArray());

            return true;
        }

    }

    public class AssemblyDependencyDeterminationContext
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string PackageDeterminationWorkspace { get; init; }
        public string TempProjFileName { get; init; }
        public string TempProjBuildOutputFolder { get; init; }
        public string ProjectAssetsJsonFile { get; init; }
        public AssemblyDependencyDeterminationContext(AssemblyName assemblyName, string targetFramework)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            PackageDeterminationWorkspace = Path.Combine(Path.GetTempPath(), $"PackageDeterminationWorkspace_{AssemblyName.Name}");
            TempProjFileName = Path.Combine(PackageDeterminationWorkspace, $"TempProject_{AssemblyName.Name}.csproj");
            TempProjBuildOutputFolder = Path.Combine(PackageDeterminationWorkspace, "release");
            ProjectAssetsJsonFile = Path.Combine(PackageDeterminationWorkspace, "obj", $@"project.assets.json");
        }
    }

    public class UnableToBuildPackageFromProjectAssetsJson : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Failed to build Package from project assets Json !";

        public Dictionary<string, object> Metadata { get; }

    }
}
