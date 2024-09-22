using FluentResults;
using System.Reflection;
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

    public static class PackageExtensions
    {
        public static string GetPersistableAssemblyName(this AssemblyName assemblyName)
        {
            return $"{assemblyName.Name}/{assemblyName.Version}";
        }

        public static AssemblyName GetAssemblyNameFromPersistableString(string value)
        {
            var nameParts = value.Split('/');
            return new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
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
