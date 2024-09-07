using Microsoft.Extensions.Configuration;
using System.Text;

namespace Baubit.Configuration
{
    /// <summary>
    /// Configuration source descriptor for <see cref="IConfiguration"/>
    /// </summary>
    public class MetaConfiguration
    {
        public List<string> RawJsonStrings { get; set; } = new List<string>();
        public List<string> JsonUriStrings { get; set; } = new List<string>();
    }

    public static class MetaConfigurationExtensions
    {
        /// <summary>
        /// Loads an <see cref="IConfiguration"/> using the given <see cref="MetaConfiguration"/>
        /// </summary>
        /// <param name="metaConfiguration">An instance of <see cref="MetaConfiguration"/></param>
        /// <returns>The built <see cref="IConfiguration"/></returns>
        public static IConfiguration Load(this MetaConfiguration metaConfiguration)
        {
            metaConfiguration?.ReplacePathPlaceholders(Application.Paths);
            var jsonUris = metaConfiguration.JsonUriStrings.Select(uriString => new Uri(uriString));

            var configurationBuilder = new ConfigurationBuilder();
            foreach (var uri in jsonUris.Where(uri => uri.IsFile))
            {
                configurationBuilder.AddJsonFile(uri.LocalPath);
            }

            var memStreams = metaConfiguration?.RawJsonStrings.Select(rawJson => new MemoryStream(Encoding.UTF8.GetBytes(rawJson)));

            foreach (var memStream in memStreams)
            {
                configurationBuilder.AddJsonStream(memStream);
            }
            var retVal = configurationBuilder.Build();
            foreach (var memStream in memStreams)
            {
                memStream.Dispose();
            }
            return retVal;
        }
        private static MetaConfiguration ReplacePathPlaceholders(this MetaConfiguration metaConfiguration, Dictionary<string, string> pathMap)
        {
            foreach (var kvp in pathMap)
            {
                for (int i = 0; i < metaConfiguration.JsonUriStrings.Count; i++)
                {
                    metaConfiguration.JsonUriStrings[i] = Path.GetFullPath(metaConfiguration.JsonUriStrings[i].Replace(kvp.Key, kvp.Value));
                }
            }
            return metaConfiguration;
        }
    }
}