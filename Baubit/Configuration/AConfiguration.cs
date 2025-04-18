using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Baubit.Configuration
{
    public abstract class AConfiguration : IValidatable
    {
        public string ValidatorKey { get; init; }
    }

    public static class ConfigurationExtensions
    {
        public static Result<TConfiguration> Load<TConfiguration>(this IConfiguration iConfiguration) where TConfiguration : AConfiguration
        {
            return Result.Try(() => iConfiguration.Get<TConfiguration>() ?? Activator.CreateInstance<TConfiguration>()!)
                         .Bind(config => config.ExpandURIs())
                         .Bind(config => config.TryValidate(config.ValidatorKey, !string.IsNullOrEmpty(config.ValidatorKey?.Trim())));
        }

        // convert AConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AConfiguration configuration, JsonSerializerOptions options)
        {
            var specificConfiguration = Convert.ChangeType(configuration, configuration.GetType());
            return JsonSerializer.Serialize(specificConfiguration, options);
        }
    }
}
