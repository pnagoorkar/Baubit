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
        public static async Task<Result<DownloadResult>> DownloadPackageAsync(PackageDownloadContext context)
        {
            return await Store.Operations
                              .DetermineDownloadablePackagesAsync(new DownloadablePackagesDeterminationContext(context.AssemblyName, context.TargetFramework))
                              .Bind(packages => Download(packages, context))
                              .Bind(packages => AddToRegistryAsync(packages, new RegistryAddContext(Application.BaubitPackageRegistry, null, context.TargetFramework)))
                              .Bind(registry => Result.Try(() => new DownloadResult(registry, registry[context.TargetFramework].First(package => package.AssemblyName.GetPersistableAssemblyName().Equals(context.AssemblyName.GetPersistableAssemblyName(), StringComparison.OrdinalIgnoreCase)))));
        }

        private static async Task<Result<List<Package>>> Download(List<Package> packages, PackageDownloadContext context)
        {
            IEnumerable<Package> downloadables = context.DownloadDependencies ? packages : packages.Where(package => package.AssemblyName.GetPersistableAssemblyName().Equals(context.AssemblyName.GetPersistableAssemblyName(), StringComparison.OrdinalIgnoreCase));

            foreach (var downloadable in downloadables)
            {
                var downloadableContext = new PackageDownloadContext(downloadable.AssemblyName, context.TargetFramework, context.TargetFolder, context.DownloadDependencies);
                var result = await FileSystem.Operations.DeleteDirectoryIfExistsAsync(new FileSystem.DirectoryDeleteContext(downloadableContext.TempDownloadPath, true))
                                                        .Bind(() => FileSystem.Operations.CreateDirectoryAsync(new FileSystem.DirectoryCreateContext(downloadableContext.TempDownloadPath)))
                                                        .Bind(() => TryBuildNugetInstallCommand(downloadableContext))
                                                        .Bind(startInfo => TryDownloadNugetPackage(startInfo, downloadableContext))
                                                        .Bind(nugetPackageFile => Compression.Operations
                                                                                             .ExtractFilesFromZipArchive(new ZipExtractFilesContext(nugetPackageFile,
                                                                                                                                                    Path.Combine(downloadableContext.TargetFolder, downloadableContext.AssemblyName.Name, downloadableContext.AssemblyName.Version.ToString()),
                                                                                                                                                    entry => entry.FullName.EndsWith($"{downloadableContext.AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase),
                                                                                                                                                    overwrite: true)));
                if (!result.IsSuccess) return Result.Fail("").WithReasons(result.Reasons);
            }
            return Result.Ok(packages);
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
                return await FileSystem.Operations
                                       .DeleteDirectoryIfExistsAsync(new FileSystem.DirectoryDeleteContext(context.TempDownloadPath, true))
                                       .Bind(() => Process.Operations
                                       .RunProcessAsync(runProcessContext)
                                       .Bind(() => ExtractDownloadFolderPathFromProcessOutput(tempProcessOutputLine))
                                       .Bind(downloadedFolder => Result.Try(() => Path.Combine(context.TempDownloadPath, downloadedFolder, $"{downloadedFolder}.nupkg"))));
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

        private static async Task<Result<List<Package>>> DownloadDependenciesIfConfigured(List<Package> packages, PackageDownloadContext context)
        {
            try
            {
                if (context.DownloadDependencies)
                {
                    var dependencies = packages.Where(package => !package.AssemblyName.GetPersistableAssemblyName().Equals(context.AssemblyName.GetPersistableAssemblyName(), StringComparison.OrdinalIgnoreCase));
                    foreach (var dependency in dependencies)
                    {
                        var dependencyContext = new PackageDownloadContext(dependency.AssemblyName, context.TargetFramework, context.TargetFolder, context.DownloadDependencies);
                        
                        await TryBuildNugetInstallCommand(dependencyContext).Bind(startInfo => TryDownloadNugetPackage(startInfo, dependencyContext))
                                                                                .Bind(nugetPackageFile => Compression.Operations
                                                                                .ExtractFilesFromZipArchive(new ZipExtractFilesContext(nugetPackageFile,
                                                                                                                                       Path.Combine(dependencyContext.TargetFolder, dependencyContext.AssemblyName.Name, dependencyContext.AssemblyName.Version.ToString()),
                                                                                                                                       entry => entry.FullName.EndsWith($"{dependencyContext.AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase),
                                                                                                                                       overwrite: true)));
                    }
                }
                return Result.Ok(packages);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class PackageDownloadContext
    {
        public AssemblyName AssemblyName { get; init; }
        public string TargetFramework { get; init; }
        public string TargetFolder { get; init; }
        public string TempDownloadPath { get; init; }
        public bool DownloadDependencies { get; init; }
        public const string AddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";

        public PackageDownloadContext(AssemblyName assemblyName, string targetFramework, string targetFolder, bool downloadDependencies)
        {
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
            TargetFolder = targetFolder;
            DownloadDependencies = downloadDependencies;
            TempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{AssemblyName.Name}");
        }
    }

    public class UndefinedOSForNugetInstall : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Undefined OS for Nuget Install !";

        public Dictionary<string, object> Metadata { get; }
    }


    public class DownloadResult
    {
        public PackageRegistry Registry { get; init; }
        public Package Package { get; init; }
        public DownloadResult(PackageRegistry registry, Package package)
        {
            this.Registry = registry;
            this.Package = package;
        }
    }
}
