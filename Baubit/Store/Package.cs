using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public class PackageRegistry2 : Dictionary<string, List<Package2>>
    {
        static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        public static Result<PackageRegistry2> ReadFrom(string filePath)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                return FileSystem.Operations
                                 .ReadFileAsync(new FileSystem.FileReadContext(filePath))
                                 .GetAwaiter()
                                 .GetResult()
                                 .Bind(jsonString => Serialization.Operations<PackageRegistry2>.DeserializeJson(new Serialization.JsonDeserializationContext<PackageRegistry2>(jsonString)))
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
            try
            {
                await Task.Yield();
                var registryReadResult = ReadFrom("");
                if (registryReadResult.IsSuccess)
                {
                    var registry = registryReadResult.Value;
                    if (registry.ContainsKey(targetFramework))
                    {
                        var package = registry[targetFramework].FirstOrDefault(p => p.AssemblyName.GetPersistableAssemblyName()
                                                                                                  .Equals(assemblyName.GetPersistableAssemblyName(),
                                                                                                          StringComparison.OrdinalIgnoreCase));
                        if (package == null)
                        {
                            return Result.Fail("Package not found !");
                        }
                        return Result.Ok(package);
                    }
                    else
                    {
                        return Result.Fail("No packages for targetFramework !");
                    }
                }
                return Result.Fail("").WithReasons(registryReadResult.Reasons);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
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
        }

        public async Task<Result> DownloadAsync(bool downloadDependencies = false)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.DownloadAsync(downloadDependencies);
            }

            if (File.Exists(DllRelativePath)) return Result.Ok();

            return await new NugetPackageDownloader(this.AssemblyName).RunAsync().Bind(nupkgFile => ExtractDllsFromNupkg(nupkgFile.EnumerateEntriesAsync()));
        }

        private async Task<Result> ExtractDllsFromNupkg(IAsyncEnumerable<ZipArchiveEntry> zipArchiveEntries)
        {
            var dllTargetPath = Path.Combine(TargetFolder, AssemblyName.Name!, AssemblyName.Version!.ToString());
            await foreach (var zipArchiveEntry in zipArchiveEntries)
            {
                if (zipArchiveEntry.FullName.EndsWith($"{AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase))
                {
                    zipArchiveEntry.ExtractToFile(dllTargetPath, true);
                }
            }
            return Result.Ok();
        }

        public async Task<Result<Assembly>> Load(AssemblyLoadContext assemblyLoadContext)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.Load(assemblyLoadContext);
            }
            return assemblyLoadContext.LoadFromAssemblyPath(DllRelativePath);
        }

        //private Result<ProcessStartInfo> BuildNugetInstallCommand()
        //{
        //    string fileName = string.Empty;
        //    IEnumerable<string> arguments = Enumerable.Empty<string>();

        //    string[] linArgs = ["/nuget.exe"];

        //    string[] commonArgs = ["install", AssemblyName.Name!,
        //                           "-O", TempDownloadPath,
        //                           "-DependencyVersion", "Ignore"];

        //    string[] versionArgs = ["-Version", AssemblyName.Version!.ToString()];

        //    if (Application.OSPlatform == OSPlatform.Windows)
        //    {
        //        fileName = "nuget";
        //        arguments = commonArgs;
        //    }
        //    else if (Application.OSPlatform == OSPlatform.Linux)
        //    {
        //        fileName = "mono";
        //        arguments = linArgs.Concat(commonArgs);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("Undefined OS for Nuget install command !");
        //    }

        //    if (AssemblyName.Version != null) arguments.Concat(versionArgs);

        //    ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments)
        //    {
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };
        //    return startInfo;
        //}

        //public const string NugetAddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";
        //private Result<SingleValueExtractionContext> BuildOutputDirectoryExtractionContext(string processOutputLine) => Result.Try(() => new SingleValueExtractionContext(processOutputLine, NugetAddedPackageLinePattern, values => values.Skip(1).First()));

        //private Result<string> DetermineDownloadedNugetPackage(string downloadedFolder) => Result.Try(() => Path.Combine(TempDownloadPath, downloadedFolder, $"{downloadedFolder}.nupkg"));


        //private Result<FileExtractionContext> BuildDllExtractionContext(string nugetPackageFile) => Result.Try(() => new FileExtractionContext(nugetPackageFile, dllTargetPath, zipExtractionCriteria, overwrite: true));
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
