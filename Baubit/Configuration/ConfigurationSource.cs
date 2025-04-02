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
        /// <summary>
        /// Loads an <see cref="IConfiguration"/> using the given <see cref="ConfigurationSource"/>
        /// </summary>
        /// <param name="configurationSource">An instance of <see cref="ConfigurationSource"/></param>
        /// <returns>The built <see cref="IConfiguration"/></returns>
        public static IConfiguration Build(this ConfigurationSource configurationSource) => configurationSource.Build(null);

        public static IConfiguration Build(this ConfigurationSource configurationSource, IConfiguration configuration)
        {
            if (configurationSource == null) return configuration;
            var configurationBuilder = new ConfigurationBuilder();
            configurationSource.AddJsonFiles(configurationBuilder).LoadResourceFiles().AddRawJsonStrings(configurationBuilder).AddSecrets(configurationBuilder);
            if (configuration != null) configurationBuilder.AddConfiguration(configuration);
            return configurationBuilder.Build();
        }
        public static Result<IConfiguration> Build2(this ConfigurationSource configurationSource, IConfiguration configuration)
        {
            var configurationBuilder = new ConfigurationBuilder();
            return Result.OkIf(configurationSource != null, "")
                         .Bind(() =>
                         {
                             configurationSource.AddJsonFiles(configurationBuilder)
                                                .LoadResourceFiles()
                                                .AddRawJsonStrings(configurationBuilder)
                                                .AddSecrets(configurationBuilder);
                             if (configuration != null) configurationBuilder.AddConfiguration(configuration);
                             return Result.Ok<IConfiguration>(configurationBuilder.Build());
                         });
        }

        private static ConfigurationSource AddJsonFiles(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            configurationSource?.ReplacePathPlaceholders(Application.Paths);
            var jsonUris = configurationSource.JsonUriStrings.Select(uriString => new Uri(uriString));

            foreach (var uri in jsonUris.Where(uri => uri.IsFile))
            {
                configurationBuilder.AddJsonFile(uri.LocalPath);
            }
            return configurationSource;
        }

        private static ConfigurationSource LoadResourceFiles(this ConfigurationSource configurationSource)
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
        }

        private static ConfigurationSource AddRawJsonStrings(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            var memStreams = configurationSource?.RawJsonStrings.Select(rawJson => new MemoryStream(Encoding.UTF8.GetBytes(rawJson)));

            foreach (var memStream in memStreams)
            {
                configurationBuilder.AddJsonStream(memStream);
            }

            return configurationSource;
        }

        private static ConfigurationSource AddSecrets(this ConfigurationSource configurationSource, ConfigurationBuilder configurationBuilder)
        {
            foreach(var localSecretsId in configurationSource.LocalSecrets)
            {
                configurationBuilder.AddUserSecrets(localSecretsId);
            }

            return configurationSource;
        }

        private static ConfigurationSource ReplacePathPlaceholders(this ConfigurationSource configurationSource, Dictionary<string, string> pathMap)
        {
            foreach (var kvp in pathMap)
            {
                for (int i = 0; i < configurationSource.JsonUriStrings.Count; i++)
                {
                    configurationSource.JsonUriStrings[i] = Path.GetFullPath(configurationSource.JsonUriStrings[i].Replace(kvp.Key, kvp.Value));
                }
            }
            return configurationSource;
        }
    }
}