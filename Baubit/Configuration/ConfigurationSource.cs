using Microsoft.Extensions.Configuration;
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
    }

    public static class ConfigurationSourceExtensions
    {
        /// <summary>
        /// Loads an <see cref="IConfiguration"/> using the given <see cref="ConfigurationSource"/>
        /// </summary>
        /// <param name="configurationSource">An instance of <see cref="ConfigurationSource"/></param>
        /// <returns>The built <see cref="IConfiguration"/></returns>
        public static IConfiguration Load(this ConfigurationSource configurationSource)
        {
            configurationSource?.ReplacePathPlaceholders(Application.Paths);
            var jsonUris = configurationSource.JsonUriStrings.Select(uriString => new Uri(uriString));

            var configurationBuilder = new ConfigurationBuilder();
            foreach (var uri in jsonUris.Where(uri => uri.IsFile))
            {
                configurationBuilder.AddJsonFile(uri.LocalPath);
            }

            var memStreams = configurationSource?.RawJsonStrings.Select(rawJson => new MemoryStream(Encoding.UTF8.GetBytes(rawJson)));

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