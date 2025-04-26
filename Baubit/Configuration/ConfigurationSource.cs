using Baubit.Configuration.Errors;
using Baubit.Configuration.Exceptions;
using Baubit.Reflection;
using Baubit.Traceability;
using Baubit.Traceability.Exceptions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
            var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
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
            try
            {
                if (obj == null) return Result.Ok(obj);

                var uriProperties = obj.GetType()
                                       .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(property => property.CustomAttributes.Any(att => att.AttributeType.Equals(typeof(URIAttribute))));

                foreach (var uriProperty in uriProperties)
                {
                    if (uriProperty.PropertyType == typeof(string))
                    {
                        var currentValue = (string)uriProperty.GetValue(obj);
                        uriProperty.SetValue(obj, currentValue.ExpandURIString().Value);
                    }
                    else if (uriProperty.PropertyType == typeof(List<string>))
                    {
                        var currentValues = (List<string>)uriProperty.GetValue(obj);
                        if (currentValues.Count > 0)
                        {
                            var newValues = currentValues.Select(val => val.ExpandURIString().ThrowIfFailed().Value).ToList();
                            uriProperty.SetValue(obj, newValues);
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported URI property type!");
                    }
                }

                return Result.Ok(obj);
            }
            catch (FailedOperationException failedOpEx)
            {
                return Result.Fail(Enumerable.Empty<IError>()).WithReasons(failedOpEx.Result.Reasons);
            }

        }

        private static Result<string> ExpandURIString(this string @value)
        {
            if (string.IsNullOrEmpty(@value)) return Result.Ok(@value);

            return Result.Try(() => Regex.Replace(@value, @"\$\{(.*?)\}", match =>
            {
                var key = match.Groups[1].Value;
                var replacement = Environment.GetEnvironmentVariable(key);
                return replacement == null ? throw new EnvironmentVariableNotFound(match.Value) : replacement;
            }), HandleMissingEnvVariables);
        }

        private static IError HandleMissingEnvVariables(Exception exp)
        {
            if (exp is EnvironmentVariableNotFound envVarNotFoundExp)
            {
                return new EnvVarNotFound(envVarNotFoundExp.EnvVariable);
            }
            else
            {
                return new ExceptionalError(exp);
            }
        }

        private static Result AddConfigurationToBuilder(this IConfigurationBuilder configurationBuilder, IConfiguration configuration)
        {
            return Result.Try(() =>
            {
                if (configuration != null) configurationBuilder.AddConfiguration(configuration);
            });
        }

        private static Result<ConfigurationSource> AddJsonFiles(this ConfigurationSource configurationSource, Microsoft.Extensions.Configuration.ConfigurationBuilder configurationBuilder)
        {
            return Result.Try(() =>
            {
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

        private static Result<ConfigurationSource> AddRawJsonStrings(this ConfigurationSource configurationSource, Microsoft.Extensions.Configuration.ConfigurationBuilder configurationBuilder)
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

        private static Result<ConfigurationSource> AddSecrets(this ConfigurationSource configurationSource, Microsoft.Extensions.Configuration.ConfigurationBuilder configurationBuilder)
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
    }
}