using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<List<Package>>> DetermineDownloadablePackagesAsync(DownloadablePackagesDeterminationContext context)
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

        private static Result<string> BuildCSProjFile(string template, DownloadablePackagesDeterminationContext context)
        {
            return Result.Try(() => template.Replace("<TARGET_FRAMEWORK>", context.TargetFramework)
                                            .Replace("<PACKAGE_NAME>", context.AssemblyName.Name)
                                            .Replace("<PACKAGE_VERSION>", context.AssemblyName.Version!.ToString()));
        }

        private static async Task<Result> WriteCSProjectToFile(string template, DownloadablePackagesDeterminationContext context)
        {
            return await FileSystem.Operations
                                   .DeleteDirectoryIfExistsAsync(new FileSystem.DirectoryDeleteContext(context.PackageDeterminationWorkspace, true))
                                   .Bind(() => FileSystem.Operations
                                                         .CreateDirectoryAsync(new FileSystem.DirectoryCreateContext(context.PackageDeterminationWorkspace)))
                                                         .Bind(() => Result.Try((Func<Task>)(async () => { await Task.Yield(); File.WriteAllText(context.TempProjFileName, template); })));
        }

        private static async Task<Result> BuildProject(DownloadablePackagesDeterminationContext context)
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

        private static async Task<Result<List<Package>>> BuildPackageFromProjectAssetsConfiguration(IConfiguration configuration, DownloadablePackagesDeterminationContext context)
        {
            try
            {
                var targetFrameworkTargets = configuration.GetSection("targets")
                                                          .GetChildren()
                                                          .FirstOrDefault(child => child.Key.Equals(context.TargetFramework));
                var packages = new List<Package>();
                var res = await BuildPackageAndDependenciesForAssembly($"{context.AssemblyName.Name}/{context.AssemblyName.Version}", targetFrameworkTargets!, packages);

                if(res.IsSuccess)
                {
                    return Result.Ok(packages);
                }
                else
                {
                    return Result.Fail("").WithReasons(res.Reasons);
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static async Task<Result> BuildPackageAndDependenciesForAssembly(string assemblyKey,
                                                                                 IConfigurationSection targetFrameworkTargets,
                                                                                 List<Package> result)
        {
            var assemblyConfigurationSection = targetFrameworkTargets!.GetChildren()
                                                                      .FirstOrDefault(child => child.Key.StartsWith(assemblyKey!, StringComparison.OrdinalIgnoreCase));

            var dllRelativePath = assemblyConfigurationSection?.GetChildren()
                                                               .FirstOrDefault(child => child.Key.Equals("runtime"))?
                                                               .GetChildren()
                                                               .FirstOrDefault()?
                                                               .Key;

            var dependencies = assemblyConfigurationSection!.GetSection("dependencies")
                                                            .GetChildren()
                                                            .Select(childSection => $"{childSection.Key}/{childSection.Value}")
                                                            .ToArray();

            foreach (var dependency in dependencies.Where(dep => !result.Any(d => d.AssemblyName.GetPersistableAssemblyName().Equals(dep, StringComparison.OrdinalIgnoreCase))))
            {
                var depResult = await BuildPackageAndDependenciesForAssembly(dependency, targetFrameworkTargets, result);
                if (!depResult.IsSuccess) return Result.Fail("").WithReasons(depResult.Reasons);
            }
            result.Add(new Package(assemblyKey, dllRelativePath, dependencies));
            return Result.Ok();
        }
    }


    public class DownloadablePackagesDeterminationContext
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string PackageDeterminationWorkspace { get; init; }
        public string TempProjFileName { get; init; }
        public string TempProjBuildOutputFolder { get; init; }
        public string ProjectAssetsJsonFile { get; init; }
        public DownloadablePackagesDeterminationContext(AssemblyName assemblyName, string targetFramework)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            PackageDeterminationWorkspace = Path.Combine(Path.GetTempPath(), $"DetermineDownloadablePackagesWorkspace_{AssemblyName.Name}");
            TempProjFileName = Path.Combine(PackageDeterminationWorkspace, $"TempProject_{AssemblyName.Name}.csproj");
            TempProjBuildOutputFolder = Path.Combine(PackageDeterminationWorkspace, "release");
            ProjectAssetsJsonFile = Path.Combine(PackageDeterminationWorkspace, "obj", $@"project.assets.json");
        }
    }
}
