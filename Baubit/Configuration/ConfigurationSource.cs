using Baubit.Reflection;
using FluentResults;
using Microsoft.Extensions.Configuration;
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
        public List<string> JsonUriStrings { get; set; } = new List<string>();
        public List<string> EmbeddedJsonResources { get; set; } = new List<string>();
        public List<string> LocalSecrets { get; init; } = new List<string>();
    }

    public static class ConfigurationSourceExtensions
    {
        public static Result<IConfiguration> Build(this ConfigurationSource configurationSource) => configurationSource.Build(null);

        public static Result<IConfiguration> Build(this ConfigurationSource configurationSource, IConfiguration configuration)
        {
            var configurationBuilder = new ConfigurationBuilder();
            return Result.OkIf(configurationSource != null, "")
                         .Bind(() => configurationSource.AddJsonFiles(configurationBuilder))
                         .Bind(configurationSource => configurationSource.LoadResourceFiles())
                         .Bind(configurationSource => configurationSource.AddRawJsonStrings(configurationBuilder))
                         .Bind(configurationSource => configurationSource.AddSecrets(configurationBuilder))
                         .Bind(configurationSource => configurationBuilder.AddConfigurationToBuilder(configuration))
                         .Bind(() => Result.Ok<IConfiguration>(configurationBuilder.Build()));
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