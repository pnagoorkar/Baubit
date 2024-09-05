using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baubit.Store
{
    public class PackageRegistry : Dictionary<string, List<Package>>
    {

    }
    public record Package
    {
        [JsonConverter(typeof(AssemblyNameJsonConverter))]
        public AssemblyName AssemblyName { get; init; }
        public string DllRelativePath { get; init; }
        [JsonIgnore]
        public string DllFile { get => Path.GetFullPath(Path.Combine(Application.BaubitRootPath, AssemblyName.Name!, AssemblyName.Version.ToString()!, DllRelativePath)); }
        public Package[] Dependencies { get; init; }

        [Obsolete("For use with serialization/deserialization only !")]
        public Package()
        {

        }

        public Package(AssemblyName assemblyName,
                       string dllRelativePath,
                       Package[] dependencies)
        {
            AssemblyName = assemblyName;
            DllRelativePath = dllRelativePath;
            Dependencies = dependencies;
        }
        public Package(string assemblyName,
                       string dllRelativePath,
                       string version,
                       string dllFile,
                       Package[] dependencies) : this(new AssemblyName($"{assemblyName}, Version={version}"), dllRelativePath, dependencies)
        {

        }
    }

    public static class PackageExtensions
    {
        public static bool TryFlatteningPackage(this Package package, List<Package> list)
        {
            if (list == null) list = new List<Package>();
            if (!list.Any(p => package.AssemblyName.Name.Equals(p.AssemblyName.Name, StringComparison.OrdinalIgnoreCase) && package.AssemblyName.Version.Equals(p.AssemblyName.Version)))
            {
                list.Add(package);
                foreach (var dep in package.Dependencies)
                {
                    dep.TryFlatteningPackage(list);
                }
            }
            return true;
        }
    }

    //public class PackageRegistryJsonConverter : JsonConverter<PackageRegistry>
    //{
    //    public override PackageRegistry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        using var jsonDocument = JsonDocument.ParseValue(ref reader);

    //        using var memoryStream = new MemoryStream();
    //        using (var utf8JsonWriter = new Utf8JsonWriter(memoryStream))
    //        {
    //            jsonDocument.WriteTo(utf8JsonWriter);
    //        }

    //        memoryStream.Seek(0, SeekOrigin.Begin);

    //        IConfiguration configuration = new ConfigurationBuilder()
    //                                           .AddJsonStream(memoryStream)
    //                                           .Build();

    //        foreach(var targetFrameworkSection in configuration.GetChildren())
    //        {
    //            string targetFramework = targetFrameworkSection.Key;
    //            var packageSections = targetFrameworkSection.GetChildren();
    //            foreach (var packageSection in targetFrameworkSection.GetChildren())
    //            {
    //                foreach (var dependencySection in packageSection.GetSection("dependencies").GetChildren())
    //                {
    //                    var actualSection = packageSections.FirstOrDefault(sec => sec["assemblyName"] == dependencySection.Value);
    //                    dependencySection.Value = actualSection.ToString();
    //                }
    //            }
    //        }
    //        return default;
    //    }

    //    public override void Write(Utf8JsonWriter writer, PackageRegistry value, JsonSerializerOptions options)
    //    {
    //        writer.WriteStartObject();
    //        foreach(var kvp in value)
    //        {
    //            writer.WritePropertyName(kvp.Key);
    //            writer.WriteRawValue(JsonSerializer.Serialize(kvp.Value, options));
    //        }
    //        writer.WriteEndObject();
    //    }
    //}

    //public class PackageDependenciesJsonConverter : JsonConverter<Package[]>
    //{
    //    public override Package[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        return null;
    //    }

    //    public override void Write(Utf8JsonWriter writer, Package[] value, JsonSerializerOptions options)
    //    {
    //        writer.WriteStartArray();
    //        foreach(var package in value)
    //        {
    //            writer.WriteStringValue($"{package.AssemblyName.Name}/{package.AssemblyName.Version}");
    //        }
    //        writer.WriteEndArray();
    //    }
    //}

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
