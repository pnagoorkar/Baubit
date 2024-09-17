using Baubit.Compression;
using Baubit.Operation;
using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Baubit.Store
{
    public sealed class DownloadPackage : IOperation<DownloadPackage.Context, DownloadPackage.Result>
    {
        private DownloadPackage()
        {

        }
        private static DownloadPackage _singletonInstance = new DownloadPackage();
        public static DownloadPackage GetInstance()
        {
            return _singletonInstance;
        }
        const string AddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";

        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                await Task.Yield();

                if (!TryResetTempDownloadPath(context, out var result)) return result;

                if (!TryBuildNugetInstallCommand(context, out var startInfo, out result)) return result;

                if (!TryRunNugetInstall(context, startInfo, out var processOutputLine, out var processErrorMessage, out result)) return result;

                if (!TryExtractingDownloadedFolderFromNugetInstallOutput(context, processOutputLine, out var downloadedFolder, out result)) return result;

                if (!TryExtractingDllsFromNupkg(context, downloadedFolder, out var extractionResult, out result)) return result;

                if(!TryDeterminingAssemblyDependencies(context, out var dependencyDeterminationResult, out result)) return result;

                if (!TryAddingPackageToRegistry(context, dependencyDeterminationResult.Value!, out var addToRegistryResult, out result)) return result;

                return new Result(true, extractionResult.Value);
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        private bool TryResetTempDownloadPath(Context context,
                                              out Result result)
        {
            result = null;
            var deleteResult = FileSystem.Operations.DeleteDirectory.RunAsync(new FileSystem.DeleteDirectory.Context(context.TempDownloadPath, true)).GetAwaiter().GetResult();
            var createResult = FileSystem.Operations.CreateDirectory.RunAsync(new FileSystem.CreateDirectory.Context(context.TempDownloadPath)).GetAwaiter().GetResult();
            if (deleteResult.Success != true || createResult.Success != true)
            {
                //result = new Result(false, "Failed to reset temp download path !", new List<object> { deleteResult, createResult });
            }
            return result == null;
        }

        private bool TryBuildNugetInstallCommand(Context context, out ProcessStartInfo startInfo, out Result result)
        {
            result = null;

            startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string winArgs = $"install {context.AssemblyName.Name} -O {context.TempDownloadPath} -DependencyVersion Ignore" + (context.AssemblyName.Version == null ? string.Empty : $" -Version {context.AssemblyName.Version}");
            string linArgs = $"/nuget.exe install {context.AssemblyName.Name} -O {context.TempDownloadPath} -DependencyVersion Ignore" + (context.AssemblyName.Version == null ? string.Empty : $" -Version {context.AssemblyName.Version}") + @" -ConfigFile /root/.nuget/NuGet/NuGet.Config";


            if (Application.OSPlatform == OSPlatform.Windows)
            {
                startInfo.FileName = "nuget";
                startInfo.Arguments = winArgs;
            }
            else if (Application.OSPlatform == OSPlatform.Linux)
            {
                startInfo.FileName = "mono";
                startInfo.Arguments = linArgs;
            }
            else
            {
                result = new Result(false, "Undefined OS for nuget install !", null);
                return false;
            }
            return true;
        }

        private bool TryRunNugetInstall(Context context, 
                                        ProcessStartInfo startInfo, 
                                        out string processOutputLine,
                                        out string processErrorMessage,
                                        out Result result)
        {
            result = null;

            var tempProcessOutputLine = string.Empty;
            var tempProcessErrorMessage = string.Empty;

            var outputDataHandler = new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data == null) return;
                Console.Write(e.Data);
                tempProcessOutputLine = string.Concat(tempProcessOutputLine, e.Data);
            });
            var errorDataHandler = new DataReceivedEventHandler((sender, e) => { tempProcessErrorMessage = string.Concat(tempProcessErrorMessage, e.Data); });

            var runProcessContext = new RunProcess.Context(startInfo, outputDataHandler, errorDataHandler);
            var runProcessResult = Process.Operations.RunProcess.RunAsync(runProcessContext).GetAwaiter().GetResult();
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
            processOutputLine = tempProcessOutputLine;
            processErrorMessage = tempProcessErrorMessage;
            return runProcessResult.Success == true;
        }

        private bool TryExtractingDownloadedFolderFromNugetInstallOutput(Context context, 
                                                                         string processOutputLine, 
                                                                         out string downloadedFolder, 
                                                                         out Result result)
        {
            result = null;
            downloadedFolder = default;

            var regexExtractContext = new Regex.Extract.Context(processOutputLine, AddedPackageLinePattern);
            var regexExtractResult = Regex.Operations.Extract.RunAsync(regexExtractContext).GetAwaiter().GetResult();

            switch(regexExtractResult.Success)
            {
                default:
                    result = new Result(new Exception("", regexExtractResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", regexExtractResult);
                    break;
                case true:
                    downloadedFolder = regexExtractResult.Value!.Skip(1).First();
                    break;
            }

            return regexExtractResult.Success == true;
        }

        private bool TryExtractingDllsFromNupkg(Context context, 
                                                string downloadedFolder, 
                                                out ExtractFilesFromArchive.Result extractionResult,
                                                out Result result)
        {
            result = null;

            var sourceNupkg = Path.Combine(context.TempDownloadPath, downloadedFolder, $"{downloadedFolder}.nupkg");
            extractionResult = Compression.Operations
                                          .ExtractFilesFromArchive
                                          .RunAsync(new Compression.ExtractFilesFromArchive.Context(sourceNupkg,
                                                                                                    Path.Combine(context.TargetFolder, context.AssemblyName.Name, context.AssemblyName.Version.ToString()),
                                                                                                    entry => entry.FullName.EndsWith($"{context.AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase),
                                                                                                    retainPaths: true,
                                                                                                    overwrite: true))
                                          .GetAwaiter()
                                          .GetResult();

            switch (extractionResult.Success)
            {
                default:
                    result = new Result(new Exception("", extractionResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", extractionResult);
                    break;
                case true:
                    break;
            }

            return extractionResult.Success == true;
        }

        private bool TryDeterminingAssemblyDependencies(Context contex,
                                                       out DetermineAssemblyDependencies.Result dependencyDeterminationResult,
                                                       out Result result)
        {
            result = null;

            dependencyDeterminationResult = Store.Operations
                                                 .DetermineAssemblyDependencies
                                                 .RunAsync(new DetermineAssemblyDependencies.Context(contex.AssemblyName, contex.TargetFramework))
                                                 .GetAwaiter()
                                                 .GetResult();

            switch (dependencyDeterminationResult.Success)
            {
                default:
                    result = new Result(new Exception("", dependencyDeterminationResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", dependencyDeterminationResult);
                    break;
                case true:
                    break;
            }

            return dependencyDeterminationResult.Success == true;
        }

        private bool TryAddingPackageToRegistry(Context context,
                                                Package package,
                                                out AddToRegistry.Result addToRegistryResult,
                                                out Result result)
        {
            result = null;
            addToRegistryResult = Operations.AddToRegistry
                                            .RunAsync(new AddToRegistry.Context(Application.BaubitPackageRegistry, package, context.TargetFramework))
                                            .GetAwaiter()
                                            .GetResult();

            switch (addToRegistryResult.Success)
            {
                default:
                    result = new Result(new Exception("", addToRegistryResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", addToRegistryResult);
                    break;
                case true:
                    break;
            }

            return addToRegistryResult.Success == true;
        }

        public sealed class Context : IContext
        {
            public AssemblyName AssemblyName { get; init; }
            public string TargetFramework { get; init; }
            public string TargetFolder { get; init; }
            public string TempDownloadPath { get; init; }

            public Context(AssemblyName assemblyName, string targetFramework, string targetFolder)
            {
                AssemblyName = assemblyName;
                TargetFramework = targetFramework;
                TargetFolder = targetFolder;
                TempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{AssemblyName.Name}");
            }
        }

        public sealed class Result : AResult<List<string>>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, List<string>? value) : base(success, value: value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }

    public static partial class Operations
    {
        public static async Task<Result<List<string>> DownloadPackageAsync(PackageDownloadContext context)
        {
            return await FileSystem.Operations.DeleteDirectoryRecursivelyAndRecreateAsync(new FileSystem.DirectoryCreateContext(context.TempDownloadPath), true)
                                              .Bind(() => { });
        }
    }

    public class PackageDownloadContext
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string TargetFolder { get; init; }
        public string TempDownloadPath { get; init; }

        public PackageDownloadContext(AssemblyName assemblyName, string targetFramework, string targetFolder)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            TargetFolder = targetFolder;
            TempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{AssemblyName.Name}");
        }
    }
}
