using Baubit.Compression;
using Baubit.Regex;
using FluentResults;
using FluentResults.Extensions;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Baubit.Store
{
    public class PackageRegistry : Dictionary<string, List<Package>>
    {
        static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        public static Result<PackageRegistry> ReadFrom(string filePath)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                return FileSystem.Operations
                                 .ReadFileAsync(new FileSystem.FileReadContext(filePath))
                                 .GetAwaiter()
                                 .GetResult()
                                 .Bind(jsonString => Serialization.Operations<PackageRegistry>.DeserializeJson(new Serialization.JsonDeserializationContext<PackageRegistry>(jsonString)))
                                 .GetAwaiter()
                                 .GetResult();

            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            finally
            {
                BaubitStoreRegistryAccessor.ReleaseMutex();
            }
        }

        public static async Task<Result<Package2>> SearchAsync(AssemblyName assemblyName, string targetFramework)
        {
            await new MockProject(assemblyName, targetFramework).BuildAsync()
                                                                .Bind(packages => ;
        }

        public Result WriteTo(string filePath)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                foreach (var key in Keys)
                {
                    this[key] = this[key].DistinctBy(package => package.AssemblyName.GetPersistableAssemblyName())
                                         .OrderBy(package => package.AssemblyName.Name)
                                         .ThenBy(package => package.AssemblyName.Version)
                                         .ThenBy(package => package.Dependencies)
                                         .ToList();
                }
                File.WriteAllText(filePath, JsonSerializer.Serialize(this, Serialization.Operations<PackageRegistry>.IndentedJsonWithCamelCase));
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            finally
            {
                BaubitStoreRegistryAccessor.ReleaseMutex();
            }
        }

    }

    public record Package
    {
        [JsonConverter(typeof(AssemblyNameJsonConverter))]
        public AssemblyName AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        [JsonIgnore]
        public string DllFile { get => Path.GetFullPath(Path.Combine(Application.BaubitRootPath, AssemblyName.Name!, AssemblyName.Version.ToString()!, DllRelativePath)); }
        public string[] Dependencies { get; init; }

        [Obsolete("For use with serialization/deserialization only !")]
        public Package()
        {

        }

        public Package(string assemblyName,
                       string dllRelativePath, 
                       string[] dependencies)
        {
            var nameParts = assemblyName.Split('/');
            AssemblyName =  new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
            DllRelativePath = dllRelativePath;
            Dependencies = dependencies;
        }
    }

    public class Package2
    {
        [JsonConverter(typeof(AssemblyNameJsonConverter))]
        public AssemblyName AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        public IReadOnlyList<Package2> Dependencies { get; init; }

        private string TempDownloadPath { get; init; }
        private string TargetFolder { get; init; }

        [Obsolete("For use with serialization/deserialization only !")]
        public Package2()
        {

        }

        private string dllTargetPath = string.Empty;
        private Expression<Func<ZipArchiveEntry, bool>> zipExtractionCriteria = null;
        public Package2(ProjectAssetsPackage projectAssetsPackage)
        {
            AssemblyName = AssemblyExtensions.GetAssemblyNameFromPersistableString(projectAssetsPackage.AssemblyName);
            var deps = new List<Package2>();
            foreach (var dependency in projectAssetsPackage.Dependencies)
            {
                deps.Add(new Package2(dependency));
            }
            Dependencies = deps;
            TempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{AssemblyName.Name}");
            dllTargetPath = Path.Combine(TargetFolder, AssemblyName.Name!, AssemblyName.Version!.ToString());
            zipExtractionCriteria = entry => entry.FullName.EndsWith($"{AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Result> DownloadAsync(bool downloadDependencies = false)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.DownloadAsync(downloadDependencies);
            }

            if (File.Exists(DllRelativePath)) return Result.Ok();

            return await BuildNugetInstallCommand().Bind(startInfo => startInfo.RunAsync())
                                                   .Bind(BuildOutputDirectoryExtractionContext)
                                                   .Bind(context => context.RunAsync())
                                                   .Bind(DetermineDownloadedNugetPackage)
                                                   .Bind(BuildDllExtractionContext)
                                                   .Bind(extractionContext => extractionContext.RunAsync())
                                                   .Bind(extractedFiles => Result.Ok());
        }

        public async Task<Result<Assembly>> Load(AssemblyLoadContext assemblyLoadContext)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.Load(assemblyLoadContext);
            }
            return assemblyLoadContext.LoadFromAssemblyPath(DllRelativePath);
        }

        private Result<ProcessStartInfo> BuildNugetInstallCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string winArgs = $"install {AssemblyName.Name} -O {TempDownloadPath} -DependencyVersion Ignore" + (AssemblyName.Version == null ? string.Empty : $" -Version {AssemblyName.Version}");
            string linArgs = $"/nuget.exe install {AssemblyName.Name} -O {TempDownloadPath} -DependencyVersion Ignore" + (AssemblyName.Version == null ? string.Empty : $" -Version {AssemblyName.Version}") + @" -ConfigFile /root/.nuget/NuGet/NuGet.Config";


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
            return startInfo;
        }

        public const string NugetAddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";
        private Result<SingleValueExtractionContext> BuildOutputDirectoryExtractionContext(string processOutputLine) => Result.Try(() => new SingleValueExtractionContext(processOutputLine, NugetAddedPackageLinePattern, values => values.Skip(1).First()));

        private Result<string> DetermineDownloadedNugetPackage(string downloadedFolder) => Result.Try(() => Path.Combine(TempDownloadPath, downloadedFolder, $"{downloadedFolder}.nupkg"));


        private Result<FileExtractionContext> BuildDllExtractionContext(string nugetPackageFile) => Result.Try(() => new FileExtractionContext(nugetPackageFile, dllTargetPath, zipExtractionCriteria, overwrite: true));
    }

    public class AssemblyNameJsonConverter : JsonConverter<AssemblyName>
    {
        public override AssemblyName? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var nameParts = reader.GetString().Split('/');
            return new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
        }

        public override void Write(Utf8JsonWriter writer, AssemblyName value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Name}/{value.Version}");
        }
    }
}
