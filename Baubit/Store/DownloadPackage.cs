using Baubit.Compression;
using FluentResults;
using FluentResults.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Baubit.Store
{
    public static partial class Operations
    {
        public static async Task<Result<Package>> DownloadPackageAsync(PackageDownloadContext context)
        {
            return await FileSystem.Operations.DeleteDirectoryRecursivelyAndRecreateAsync(new FileSystem.DirectoryCreateContext(context.TempDownloadPath), true)
                                              .Bind(() => TryBuildNugetInstallCommand(context))
                                              .Bind(startInfo => TryDownloadNugetPackage(startInfo, context))
                                              .Bind(nugetPackageFile => Compression.Operations
                                                                                   .ExtractFilesFromZipArchive(new ZipExtractFilesContext(nugetPackageFile,
                                                                                                                                          Path.Combine(context.TargetFolder, context.AssemblyName.Name, context.AssemblyName.Version.ToString()),
                                                                                                                                          entry => entry.FullName.EndsWith($"{context.AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase),
                                                                                                                                          overwrite: true)))
                                              .Bind(dllFiles => Store.Operations.DetermineAssemblyDependenciesAsync(new AssemblyDependencyDeterminationContext(context.AssemblyName, context.TargetFramework)))
                                              .Bind(package => Store.Operations.AddToRegistryAsync(new RegistryAddContext(Application.BaubitPackageRegistry, package, context.TargetFramework))
                                              .Bind(() => Result.Try((Func<Task<Package>>)(async () => { await Task.Yield(); return package; }))));
        }

        private static async Task<Result<ProcessStartInfo>> TryBuildNugetInstallCommand(PackageDownloadContext context)
        {
            await Task.Yield();
            ProcessStartInfo startInfo = new ProcessStartInfo
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
                return Result.Fail(new UndefinedOSForNugetInstall());
            }
            return Result.Ok(startInfo);
        }

        private static async Task<Result<string>> TryDownloadNugetPackage(ProcessStartInfo processStartInfo, PackageDownloadContext context)
        {
            try
            {
                var tempProcessOutputLine = string.Empty;
                var tempProcessErrorMessage = string.Empty;

                var outputDataHandler = new DataReceivedEventHandler((sender, e) =>
                {
                    if (e.Data == null) return;
                    Console.Write(e.Data);
                    tempProcessOutputLine = string.Concat(tempProcessOutputLine, e.Data);
                });
                var errorDataHandler = new DataReceivedEventHandler((sender, e) => { tempProcessErrorMessage = string.Concat(tempProcessErrorMessage, e.Data); });

                var runProcessContext = new Process.ProcessRunContext(processStartInfo, outputDataHandler, errorDataHandler);
                return await Process.Operations
                                    .RunProcessAsync(runProcessContext)
                                    .Bind(() => ExtractDownloadFolderPathFromProcessOutput(tempProcessOutputLine))
                                    .Bind(downloadedFolder => Result.Try(() => Path.Combine(context.TempDownloadPath, downloadedFolder, $"{downloadedFolder}.nupkg")));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private static async Task<Result<string>> ExtractDownloadFolderPathFromProcessOutput(string output)
        {
            return await Regex.Operations.ExtractAsync(new Regex.RegexExtractContext(output, PackageDownloadContext.AddedPackageLinePattern))
                                         .Bind(values => Result.Try(() => values.Skip(1).First()));
        }
    }

    public class PackageDownloadContext
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string TargetFolder { get; init; }
        public string TempDownloadPath { get; init; }
        public const string AddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";

        public PackageDownloadContext(AssemblyName assemblyName, string targetFramework, string targetFolder)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            TargetFolder = targetFolder;
            TempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{AssemblyName.Name}");
        }
    }

    public class UndefinedOSForNugetInstall : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Undefined OS for Nuget Install !";

        public Dictionary<string, object> Metadata { get; }
    }
}
