using Baubit.Configuration;
using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baubit.Store
{
    public class PackageRegistry2 : Dictionary<string, List<Package2>>
    {
        static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        public static Result<PackageRegistry2> ReadFrom(string filePath)
        {
            try
            {
                BaubitStoreRegistryAccessor.WaitOne();
                if (File.Exists(filePath))
                {
                    var registryConfiguration = new MetaConfiguration { JsonUriStrings = [filePath] }.Load();
                    var targetFrameworkSection = registryConfiguration.GetSection(Application.TargetFramework);
                    var buildResult = Package2.BuildPackageTrees(targetFrameworkSection);
                    if (buildResult.IsSuccess)
                    {
                        return Result.Ok(new PackageRegistry2 { { Application.TargetFramework, buildResult.Value } });
                    }
                    else
                    {
                        return Result.Fail("").WithReasons(buildResult.Reasons);
                    }
                }
                else
                {
                    return new PackageRegistry2();
                }
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

        public static async Task<Result> Add(string targetFramework, Package2 package)
        {
            try
            {
                await Task.Yield();
                var readResult = ReadFrom(Application.BaubitPackageRegistry);
                if (!readResult.IsSuccess)
                {
                    return Result.Fail("").WithReasons(readResult.Reasons);
                }
                var registerablePackages = new List<Package2>();
                var flatteningResult = Package2.TryFlatteningPackage(package, registerablePackages);
                foreach (var registerablePackage in registerablePackages)
                {
                    var packageAddResult = readResult.Value.AddOrUpdate(targetFramework, registerablePackage);
                    if (!packageAddResult.IsSuccess)
                    {
                        return Result.Fail("").WithReasons(packageAddResult.Reasons);
                    }
                }
                var writeResult = readResult.Value.WriteTo(Application.BaubitPackageRegistry);
                if (!writeResult.IsSuccess)
                {
                    return Result.Fail("").WithReasons(writeResult.Reasons);
                }
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private Result AddOrUpdate(string targetFramework, Package2 package)
        {
            try
            {
                if (ContainsKey(targetFramework))
                {
                    if (this[targetFramework].Any(p => p.AssemblyName.GetPersistableAssemblyName().Equals(package.AssemblyName.GetPersistableAssemblyName(), StringComparison.OrdinalIgnoreCase)))
                    {
                        //already exists. Do nothing
                    }
                    else
                    {
                        this[targetFramework].Add(package);
                    }
                }
                else
                {
                    Add(targetFramework, [package]);
                }
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        public static async Task<Result<Package2>> SearchAsync(AssemblyName assemblyName, string targetFramework)
        {
            try
            {
                await Task.Yield();
                var registryReadResult = ReadFrom(Application.BaubitPackageRegistry);
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
                File.WriteAllText(filePath, JsonSerializer.Serialize(this, Application.IndentedJsonWithCamelCase));
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
        private string TargetFolder { get; init; }
        public string DllRelativePath { get; init; }
        [JsonIgnore]
        public string DllFile { get; init; }
        [JsonConverter(typeof(Package2JsonConverter))]
        public IReadOnlyList<Package2> Dependencies { get; init; }

        [Obsolete("For use with serialization/deserialization only !")]
        public Package2()
        {

        }

        public Package2(AssemblyName assemblyName, string dllRelativePath, List<Package2> dependencies)
        {
            AssemblyName = assemblyName;
            DllRelativePath = dllRelativePath;
            Dependencies = dependencies.AsReadOnly();
            TargetFolder = Path.Combine(Application.BaubitRootPath, AssemblyName.Name!, AssemblyName.Version.ToString()!);
            DllFile = Path.GetFullPath(Path.Combine(TargetFolder, DllRelativePath));
        }

        public static Result<Package2> BuildPackage(ProjectAssetsPackage projectAssetsPackage)
        {
            var assemblyName = AssemblyExtensions.GetAssemblyNameFromPersistableString(projectAssetsPackage.AssemblyName);
            var dllRelativePath = projectAssetsPackage.DllRelativePath;
            var dependencies = new List<Package2>();
            foreach (var dependency in projectAssetsPackage.Dependencies)
            {
                dependencies.Add(BuildPackage(dependency).Value);
            }
            return new Package2(assemblyName, dllRelativePath, dependencies);
        }

        public static Result<Package2> BuildPackage(IConfigurationSection packageSection,
                                                    IConfigurationSection targetFrameworkSection,
                                                    List<Package2> packages)
        {
            var assemblyName = AssemblyExtensions.GetAssemblyNameFromPersistableString(packageSection["assemblyName"]!);
            var dllRelativePath = packageSection["dllRelativePath"]!;
            var dependencies = new List<Package2>();

            foreach (var dependency in packageSection.GetSection("dependencies").GetChildren())
            {
                var searchResult = packages.Search(package => package.AssemblyName.GetPersistableAssemblyName().Equals(dependency.Value, StringComparison.OrdinalIgnoreCase));
                var dependencyPackage = searchResult.ValueOrDefault;

                if (dependencyPackage == null)
                {
                    var dependencyPackageSection = targetFrameworkSection.GetChildren()
                                                                         .FirstOrDefault(child => child["assemblyName"].Equals(dependency.Value, StringComparison.OrdinalIgnoreCase));
                    if (dependencyPackageSection != null && dependencyPackageSection.Exists())
                    {
                        var depBuildResult = BuildPackage(dependencyPackageSection, targetFrameworkSection, packages);
                        if (depBuildResult.IsSuccess)
                        {
                            dependencyPackage = depBuildResult.Value;
                        }
                        else
                        {
                            return Result.Fail("").WithReasons(depBuildResult.Reasons);
                        }
                    }
                    else
                    {
                        return Result.Fail($"Dependency section does not exist for {assemblyName.GetPersistableAssemblyName()}");
                    }
                }
                dependencies.Add(dependencyPackage);
            }
            var currentPackage = new Package2(assemblyName, dllRelativePath, dependencies);
            packages.Add(currentPackage);
            return Result.Ok(currentPackage);
        }

        public static Result<List<Package2>> BuildPackageTrees(IConfigurationSection targetFrameworkSection)
        {
            List<Package2> packages = new List<Package2>();

            foreach (var packageSection in targetFrameworkSection.GetChildren())
            {
                if (packages.Any(package => package.AssemblyName.GetPersistableAssemblyName().Equals(packageSection["assemblyName"], StringComparison.OrdinalIgnoreCase))) continue;
                var buildResult = BuildPackage(packageSection, targetFrameworkSection, packages);
                if (buildResult.IsSuccess)
                {
                    //All good. Do nothing
                }
                else
                {
                    return Result.Fail("").WithReasons(buildResult.Reasons);
                }
            }
            return Result.Ok(packages);
        }

        public static Result TryFlatteningPackage(Package2 package, List<Package2> list)
        {
            if (list == null) list = new List<Package2>();

            if (!list.Any(p => package.AssemblyName.Name.Equals(p.AssemblyName.Name, StringComparison.OrdinalIgnoreCase) && 
                               package.AssemblyName.Version.Equals(p.AssemblyName.Version)))
            {
                list.Add(package);
                foreach (var dep in package.Dependencies)
                {
                    TryFlatteningPackage(dep, list);
                }
            }
            return Result.Ok();
        }

        public async Task<Result> DownloadAsync(bool downloadDependencies = false)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.DownloadAsync(downloadDependencies);
            }

            if (File.Exists(DllFile)) return Result.Ok();

            return await NugetPackageDownloader.BuildAsync(this.AssemblyName)
                                               .Bind(downloader => downloader.RunAsync())
                                               .Bind(nupkgFile => ExtractDllsFromNupkg(nupkgFile.EnumerateEntriesAsync()));
        }

        private async Task<Result> ExtractDllsFromNupkg(IAsyncEnumerable<ZipArchiveEntry> zipArchiveEntries)
        {
            await foreach (var zipArchiveEntry in zipArchiveEntries)
            {
                if (zipArchiveEntry.FullName.EndsWith($"{AssemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase))
                {
                    string destinationFileName = Path.GetFullPath(Path.Combine(TargetFolder, zipArchiveEntry.FullName));
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFileName));
                    zipArchiveEntry.ExtractToFile(destinationFileName, true);
                }
            }
            return Result.Ok();
        }

        public async Task<Result<Assembly>> LoadAsync(AssemblyLoadContext assemblyLoadContext)
        {
            foreach (var dependency in Dependencies)
            {
                await dependency.LoadAsync(assemblyLoadContext);
            }
            return assemblyLoadContext.LoadFromAssemblyPath(DllFile);
        }
    }

    public static class PackageExtensions
    {
        public static Result<Package2> Search(this IEnumerable<Package2> packages, Expression<Func<Package2, bool>> criteria)
        {
            foreach (var package in packages)
            {
                if (criteria.Compile()(package))
                {
                    return Result.Ok(package);
                }
                else
                {
                    var depSearchResult = package.Dependencies.Search(criteria);
                    if (depSearchResult.IsSuccess) return depSearchResult;
                }
            }
            return Result.Fail("Package not found !");
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

    public class Package2JsonConverter : JsonConverter<IReadOnlyList<Package2>>
    {
        public override IReadOnlyList<Package2>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<Package2> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach(var package in value)
            {
                writer.WriteStringValue(package.AssemblyName.GetPersistableAssemblyName());
            }
            writer.WriteEndArray();
        }
    }
}
