using Baubit.Reflection;
using Baubit.Traceability;
using FluentResults;
using System.Text;
using System.Text.Json;

namespace Baubit.DI
{
    public static class ModuleExtensions
    {
        public static Result<List<IModule>> TryFlatten<TModule>(this TModule module) where TModule : IModule
        {
            return Result.Try(() => new List<IModule>())
                         .Bind(modules => module.TryFlatten(modules) ? Result.Ok(modules) : Result.Fail(""));
        }
        public static bool TryFlatten<TModule>(this TModule module, List<IModule> modules) where TModule : IModule
        {
            if (modules == null) modules = new List<IModule>();

            modules.Add(module);

            foreach (var nestedModule in module.NestedModules)
            {
                nestedModule.TryFlatten(modules);
            }

            return true;
        }

        public static Result<string> Serialize<TModule>(this TModule module,
                                                        JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = jsonSerializerOptions?.WriteIndented == true });
                return module.Serialize(writer, jsonSerializerOptions)
                             .Bind(writer => Result.Try(() =>
                             {
                                 writer.Flush();
                                 return Encoding.UTF8.GetString(stream.ToArray());
                             }))
                             .ThrowIfFailed()
                             .Value;
            });
        }

        private static Result<Utf8JsonWriter> Serialize<TModule>(this TModule module,
                                                                 Utf8JsonWriter writer,
                                                                 JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                writer.WriteStartObject();
                if (module is IRootModule rootModule)
                {
                    writer.WritePropertyName("rootModule");
                    writer.WriteStartObject();
                    module.WriteModuleDescriptor(writer, jsonSerializerOptions);
                    writer.WriteEndObject();
                }
                else
                {

                    module.WriteModuleDescriptor(writer, jsonSerializerOptions);
                }

                writer.WriteEndObject();
                return writer;
            });
        }

        private static Result<Utf8JsonWriter> WriteModuleDescriptor<TModule>(this TModule module,
                                                                             Utf8JsonWriter writer,
                                                                             JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                writer.WriteString("type", module.GetType().GetBaubitFormattedAssemblyQualifiedName().ThrowIfFailed().Value);
                writer.WritePropertyName("configuration");
                using var configJson = JsonDocument.Parse(JsonSerializer.Serialize(module.Configuration, jsonSerializerOptions));
                writer.WriteStartObject();

                foreach (var property in configJson.RootElement.EnumerateObject())
                {
                    property.WriteTo(writer); // copy all properties as-is
                }

                writer.WritePropertyName("moduleConstraints");
                writer.WriteStartArray();

                foreach (var constraint in module.Constraints)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", constraint.GetType().GetBaubitFormattedAssemblyQualifiedName().ThrowIfFailed().Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WritePropertyName("modules");
                writer.WriteStartArray();

                module.NestedModules.Aggregate(Result.Ok(writer), (seed, next) => seed.Bind(w => next.Serialize(w, jsonSerializerOptions)));

                writer.WriteEndArray();

                writer.WriteEndObject();
                return writer;
            });
        }
    }
}
