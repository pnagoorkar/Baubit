using Baubit.Operation;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Baubit.Store
{
    public class DetermineAssemblyDependencies : IOperation<DetermineAssemblyDependencies.Context, DetermineAssemblyDependencies.Result>
    {
        private DetermineAssemblyDependencies()
        {

        }
        private static DetermineAssemblyDependencies _singletonInstance = new DetermineAssemblyDependencies();
        public static DetermineAssemblyDependencies GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                if (!TryReadCSProjTemplate(context, out var readResourceResult, out var result)) return result;

                string csProjTemplate = readResourceResult.Value
                                                          .Replace("<TARGET_FRAMEWORK>", context.TargetFramework)
                                                          .Replace("<PACKAGE_NAME>", context.AssemblyName.Name)
                                                          .Replace("<PACKAGE_VERSION>", context.AssemblyName.Version.ToString());

                if (!TryCreateTempDirectory(context, out result)) return result;

                File.WriteAllText(context.TempProjFileName, csProjTemplate);

                if (!TryDotnetBuild(context, out result)) return result;

                var projectAssets = new ConfigurationBuilder().AddJsonFile(context.ProjectAssetsJsonFile).Build();

                var targetFrameworkTargets = projectAssets.GetSection("targets")
                                                          .GetChildren()
                                                          .FirstOrDefault(child => child.Key.Equals(context.TargetFramework));

                var rootAssemblyTarget = targetFrameworkTargets.GetChildren()
                                                               .FirstOrDefault(child => child.Key.StartsWith(context.AssemblyName.Name, StringComparison.OrdinalIgnoreCase));

                if (TryConvertingConfigSectionToPackage(rootAssemblyTarget, targetFrameworkTargets.GetChildren().ToArray(), out var package))
                {
                    return new Result(true, package);
                }
                else
                {
                    return new Result(false, "", null);
                }
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
            finally
            {
                //TryDeleteTempDirectory(context);
            }
        }

        private bool TryReadCSProjTemplate(Context context, out Resource.ReadEmbeddedResource.Result readResourceResult, out Result result)
        {
            result = null;
            readResourceResult = Resource.Operations
                             .ReadEmbeddedResource
                             .RunAsync(new Resource.ReadEmbeddedResource.Context($"{this.GetType().Namespace}.CSProjTemplate.txt",
                                                                                 Assembly.GetExecutingAssembly()))
                             .GetAwaiter()
                             .GetResult();
            switch (readResourceResult.Success)
            {
                default:
                    result = new Result(new Exception("", readResourceResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", readResourceResult);
                    break;
                case true: break;
            }
            return readResourceResult.Success == true;
        }

        private bool TryCreateTempDirectory(Context context, out Result result)
        {
            result = null;
            var createDirectoryResult = FileSystem.Operations.CreateDirectory.RunAsync(new FileSystem.CreateDirectory.Context(context.PackageDeterminationWorkspace)).GetAwaiter().GetResult();
            switch (createDirectoryResult.Success)
            {
                default:
                    result = new Result(new Exception("", createDirectoryResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", createDirectoryResult);
                    break;
                case true: break;
            }
            return createDirectoryResult.Success == true;
        }

        private bool TryDeleteTempDirectory(Context context)
        {
            var deleteDirectoryResult = FileSystem.Operations
                                                  .DeleteDirectory
                                                  .RunAsync(new FileSystem.DeleteDirectory.Context(context.PackageDeterminationWorkspace, true))
                                                  .GetAwaiter()
                                                  .GetResult();
            return deleteDirectoryResult.Success == true;
        }

        private bool TryDotnetBuild(Context context, out Result result)
        {
            result = null;
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {context.TempProjFileName} --configuration Debug --output {context.TempProjBuildOutputFolder}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var runProcessResult = Process.Operations.RunProcess.RunAsync(new Process.RunProcess.Context(startInfo, null, null)).GetAwaiter().GetResult();
            switch (runProcessResult.Success)
            {
                default:
                    result = new Result(new Exception("", runProcessResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", runProcessResult);
                    break;
                case true: break;
            }
            return runProcessResult.Success == true;
        }

        public bool TryConvertingConfigSectionToPackage(IConfigurationSection rootConfigurationSection, 
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

        public sealed class Context : IContext
        {
            public AssemblyName AssemblyName { get; init; }
            public string TargetFramework { get; init; }
            public string PackageDeterminationWorkspace { get; init; }
            public string TempProjFileName { get; init; }
            public string TempProjBuildOutputFolder { get; init; }
            public string ProjectAssetsJsonFile { get; init; }
            public Context(AssemblyName assemblyName, string targetFramework)
            {
                AssemblyName = assemblyName;
                TargetFramework = targetFramework;
                PackageDeterminationWorkspace = Path.Combine(Path.GetTempPath(), $"PackageDeterminationWorkspace_{AssemblyName.Name}");
                TempProjFileName = Path.Combine(PackageDeterminationWorkspace, $"TempProject_{AssemblyName.Name}.csproj");
                TempProjBuildOutputFolder = Path.Combine(PackageDeterminationWorkspace, "release");
                ProjectAssetsJsonFile = Path.Combine(PackageDeterminationWorkspace, "obj", $@"project.assets.json");
            }
        }

        public sealed class Result : AResult<Package>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, Package? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
