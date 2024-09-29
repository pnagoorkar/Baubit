using FluentResults;
using FluentResults.Extensions;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baubit.Store
{
    public class Package : IEquatable<Package>, IEquatable<string>
    {
        [JsonConverter(typeof(AssemblyNameJsonConverter))]
        public AssemblyName AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        [JsonIgnore]
        public string PackageRoot { get; init; }
        [JsonIgnore]
        public string DllFile { get; init; }

        [JsonConverter(typeof(PackageJsonConverter))]
        public IReadOnlyList<Package> Dependencies { get; init; }

        [Obsolete("For use with serialization/deserialization only !")]
        public Package()
        {

        }

        private Package(string packageRoot,
                        string assemblyName,
                        string dllRelativePath,
                        List<Package> dependencies)
        {
            PackageRoot = packageRoot;
            AssemblyName = AssemblyExtensions.GetAssemblyNameFromPersistableString(assemblyName);
            DllRelativePath = dllRelativePath;
            Dependencies = dependencies.AsReadOnly();
            DllFile = Path.GetFullPath(Path.Combine(PackageRoot, DllRelativePath));
        }

        public static Package Build(string packageRoot, string assemblyName, string dllRelativePath, List<Package> dependencies)
        {
            return new Package(packageRoot, assemblyName, dllRelativePath, dependencies);
        }

        public bool Equals(Package? other) => Equals(other?.AssemblyName.GetPersistableAssemblyName());

        public bool Equals(string? other) => other != null && other.Equals(AssemblyName.GetPersistableAssemblyName(), StringComparison.OrdinalIgnoreCase);
    }

    public class SerializablePackage
    {
        public string AssemblyName { get; init; }
        public string DllRelativePath { get; set; }
        public List<string> Dependencies { get; init; } = new List<string>();
    }

    public static class PackageExtnsns
    {
        public static SerializablePackage AsSerializable(this Package package)
        {
            return new SerializablePackage
            {
                AssemblyName = package.AssemblyName.GetPersistableAssemblyName(),
                DllRelativePath = package.DllRelativePath,
                Dependencies = package.Dependencies.Select(dep => dep.AssemblyName.GetPersistableAssemblyName()).ToList(),
            };
        }

        public static IEnumerable<SerializablePackage> AsSerializable(this IEnumerable<Package> packages)
        {
            return packages.GetAllTrees().Select(p => p.AsSerializable());
        }

        public static IEnumerable<Package> GetAllTrees(this Package package) 
        {
            yield return package;

            foreach (var dependency in package.Dependencies.GetAllTrees())
            {
                yield return dependency;
            }
        }

        public static IEnumerable<Package> GetAllTrees(this IEnumerable<Package> packages)
        {
            return packages.SelectMany(package => package.GetAllTrees()).Distinct();
        }

        public static Package? Search(this Package package, AssemblyName assemblyName)
        {
            return package.Search(assemblyName);
        }

        public static Package? Search(this Package package, string assemblyName)
        {
            return package.GetAllTrees().FirstOrDefault(pkg => pkg.AssemblyName.GetPersistableAssemblyName().Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static Package? Search(this IEnumerable<Package> packages, AssemblyName assemblyName)
        {
            return packages.Search(assemblyName.GetPersistableAssemblyName());
        }

        public static Package? Search(this IEnumerable<Package> packages, string assemblyName)
        {
            return packages.GetAllTrees().FirstOrDefault(pkg => pkg.AssemblyName.GetPersistableAssemblyName().Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static SerializablePackage? Search(this IEnumerable<SerializablePackage> serializablePackages, AssemblyName assemblyName)
        {
            return serializablePackages.Search(assemblyName.GetPersistableAssemblyName());
        }

        public static SerializablePackage? Search(this IEnumerable<SerializablePackage> serializablePackages, string assemblyName)
        {
            return serializablePackages.FirstOrDefault(serializablePackage => serializablePackage.AssemblyName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        public static Package AsPackage(this SerializablePackage serializablePackage,
                                        IEnumerable<SerializablePackage> serializablePackages, 
                                        List<Package> cache)
        {
            var dependencies = new List<Package>();

            foreach (var serializableDependency in serializablePackage.Dependencies)
            {
                var cachedPackage = cache.Search(serializableDependency);
                var dependencyPackage = cachedPackage ?? serializablePackages.Search(serializableDependency)!.AsPackage(serializablePackages, cache);
                dependencies.Add(dependencyPackage);
                if (cachedPackage == null) cache.Add(dependencyPackage);
            }

            var currentPackage = Package.Build(AssemblyExtensions.GetAssemblyNameFromPersistableString(serializablePackage.AssemblyName).GetPackageRootPath(), 
                                               serializablePackage.AssemblyName, 
                                               serializablePackage.DllRelativePath, 
                                               dependencies);
            return currentPackage;
        }

        public static List<Package> AsPackages(this IEnumerable<SerializablePackage> serializablePackages)
        {
            var result = new List<Package>();
            foreach (var serializablePackage in serializablePackages)
            {
                var cachedPackage = result.Search(serializablePackage.AssemblyName);
                var package = cachedPackage ?? serializablePackage.AsPackage(serializablePackages, result);

                if(cachedPackage == null) result.Add(package);
            }
            return result;
        }

        public static async Task<Result> DownloadAsync(this Package package, bool downloadDependencies = false)
        {
            try
            {
                foreach (var dependency in package.Dependencies)
                {
                    await dependency.DownloadAsync(downloadDependencies);
                }

                if (File.Exists(package.DllFile)) return Result.Ok();

                var downloadResult = await NugetPackageDownloader.BuildAsync(package.AssemblyName)
                                                                 .Bind(downloader => downloader.RunAsync())
                                                                 .Bind(nupkgFile => Result.Try(() => nupkgFile.EnumerateEntriesAsync()));

                if(downloadResult.IsFailed)
                {
                    return Result.Fail("").WithReasons(downloadResult.Reasons);
                }
                await foreach (var zipArchiveEntry in downloadResult.Value)
                {
                    if (zipArchiveEntry.FullName.EndsWith($"{package.AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        string destinationFileName = Path.GetFullPath(Path.Combine(package.PackageRoot, zipArchiveEntry.FullName));
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFileName));
                        zipArchiveEntry.ExtractToFile(destinationFileName, true);
                    }
                }
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        public static async Task<Result<Assembly>> LoadAsync(this Package package, AssemblyLoadContext assemblyLoadContext)
        {
            foreach (var dependency in package.Dependencies)
            {
                await dependency.LoadAsync(assemblyLoadContext);
            }
            return assemblyLoadContext.LoadFromAssemblyPath(package.DllFile);
        }
    }

    public class PackageJsonConverter : JsonConverter<IReadOnlyList<Package>>
    {
        public override IReadOnlyList<Package>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<Package> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var package in value)
            {
                writer.WriteStringValue(package.AssemblyName.GetPersistableAssemblyName());
            }
            writer.WriteEndArray();
        }
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
