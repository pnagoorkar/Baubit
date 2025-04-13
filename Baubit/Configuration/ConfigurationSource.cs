using Baubit.Reflection;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Baubit.Configuration
{
    /// <summary>
    /// Configuration source descriptor for <see cref="IConfiguration"/>
    /// </summary>
    public class ConfigurationSource
    {
        public List<string> RawJsonStrings { get; set; } = new List<string>();
        [URI]
        public List<string> JsonUriStrings { get; set; } = new List<string>();
        [URI]
        public List<string> EmbeddedJsonResources { get; set; } = new List<string>();
        [URI]
        public List<string> LocalSecrets { get; init; } = new List<string>();
    }

    public static class ConfigurationSourceExtensions
    {
        public static Result<IConfiguration> Build(this ConfigurationSource configurationSource) => configurationSource.Build(null);

        public static Result<IConfiguration> Build(this ConfigurationSource configurationSource, IConfiguration configuration)
        {
            var configurationBuilder = new ConfigurationBuilder();
            return Result.OkIf(configurationSource != null, "")
                         .Bind(() => configurationSource.ExpandURIs())
                         .Bind(configSource => configurationSource.AddJsonFiles(configurationBuilder))
                         .Bind(configurationSource => configurationSource.LoadResourceFiles())
                         .Bind(configurationSource => configurationSource.AddRawJsonStrings(configurationBuilder))
                         .Bind(configurationSource => configurationSource.AddSecrets(configurationBuilder))
                         .Bind(configurationSource => configurationBuilder.AddConfigurationToBuilder(configuration))
                         .Bind(() => Result.Ok<IConfiguration>(configurationBuilder.Build()));
        }

        public static Result<T> ExpandURIs<T>(this T obj)
        {
            var uriDic = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value);

            var uriProperties = obj.GetType()
                                   .GetProperties()
                                   .Where(property => property.CustomAttributes.Any(att => att.AttributeType.Equals(typeof(URIAttribute))));

            foreach (var uriProperty in uriProperties)
            {

                if (uriProperty.PropertyType.IsAssignableTo(typeof(string)))
                {
                    var currentValue = (string)uriProperty.GetValue(obj);

                    uriProperty.SetValue(obj, currentValue.ExpandURIString(uriDic).Value);
                }
                else if (uriProperty.PropertyType.IsAssignableTo(typeof(List<string>)))
                {
                    var currentValues = (List<string>)uriProperty.GetValue(obj);
                    var newValues = currentValues.Select(val => val.ExpandURIString(uriDic).Value).ToList();

                    uriProperty.SetValue(obj, newValues);
                }
                else
                {
                    throw new Exception("Unsupported URI property type!");
                }
            }

            return Result.Ok<T>(obj);

        }

        private static Result<string> ExpandURIString(this string @value, Dictionary<string, string> uriDic)
        {
            return Result.Try(() => uriDic.Aggregate(@value, (seed, next) => seed.Replace($"${{{next.Key}}}", next.Value)));
        }

        private static Result AddConfigurationToBuilder(this IConfigurationBuilder configurationBuilder, IConfiguration configuration)
        {
            return Result.Try(() =>
            {
                if (configuration != null) configurationBuilder.AddConfiguration(configuration);
            });
        }

        private static Result<ConfigurationSource> AddJsonFiles(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            return Result.Try(() =>
            {
                configurationSource?.ReplacePathPlaceholders(Application.Paths);
                var jsonUris = configurationSource.JsonUriStrings.Select(uriString => new Uri(uriString));

                foreach (var uri in jsonUris.Where(uri => uri.IsFile))
                {
                    configurationBuilder.AddJsonFile(uri.LocalPath);
                }
                return configurationSource;
            });
        }

        private static Result<ConfigurationSource> LoadResourceFiles(this ConfigurationSource configurationSource)
        {
            return Result.Try(() =>
            {
                foreach (var embeddedJsonResource in configurationSource.EmbeddedJsonResources)
                {
                    var identifierParts = embeddedJsonResource.Split(';');
                    var assemblyNamePart = identifierParts[0];
                    var fileNamePart = identifierParts[1];

                    AssemblyName assemblyName;
                    if (assemblyNamePart.Contains("/"))
                    {
                        assemblyName = Reflection.AssemblyExtensions.GetAssemblyNameFromPersistableString(assemblyNamePart);
                    }
                    else
                    {
                        assemblyName = new AssemblyName(assemblyNamePart);
                    }
                    var resourceName = $"{assemblyName.Name}.{fileNamePart}";

                    var readResult = assemblyName.TryResolveAssembly()?.ReadResource(resourceName).GetAwaiter().GetResult();

                    if (readResult?.IsSuccess != true) throw new Exception($"Failed to read embedded json resource {embeddedJsonResource}");
                    configurationSource.RawJsonStrings.Add(readResult.Value);
                }
                return configurationSource;
            });
        }

        private static Result<ConfigurationSource> AddRawJsonStrings(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            return Result.Try(() =>
            {
                var memStreams = configurationSource.RawJsonStrings.Select(rawJson => new MemoryStream(Encoding.UTF8.GetBytes(rawJson)));

                foreach (var memStream in memStreams)
                {
                    configurationBuilder.AddJsonStream(memStream);
                }

                return configurationSource;
            });
        }

        private static Result<ConfigurationSource> AddSecrets(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            return Result.Try(() =>
            {
                foreach (var localSecretsId in configurationSource.LocalSecrets)
                {
                    configurationBuilder.AddUserSecrets(localSecretsId);
                }

                return configurationSource;
            });
        }

        private static Result<ConfigurationSource> ReplacePathPlaceholders(this ConfigurationSource configurationSource, Dictionary<string, string> pathMap)
        {
            return Result.Try(() =>
            {
                foreach (var kvp in pathMap)
                {
                    for (int i = 0; i < configurationSource.JsonUriStrings.Count; i++)
                    {
                        configurationSource.JsonUriStrings[i] = Path.GetFullPath(configurationSource.JsonUriStrings[i].Replace(kvp.Key, kvp.Value));
                    }
                }
                return configurationSource;
            });
        }
    }
}