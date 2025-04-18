using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Baubit.Configuration
{
    public abstract class AConfiguration : IValidatable
    {
        public string ValidatorKey { get; init; } = "default";
    }

    public static class ConfigurationExtensions
    {
        public static Result<TConfiguration> Load<TConfiguration>(this IConfiguration iConfiguration) where TConfiguration : AConfiguration
        {
            return Result.Try(() => iConfiguration.Get<TConfiguration>() ?? Activator.CreateInstance<TConfiguration>()!)
                         .Bind(config => config.ExpandURIs())
                         .Bind(config => config.Validate());
        }
        public static Result<TConfiguration> Validate<TConfiguration>(this TConfiguration configuration) where TConfiguration : AConfiguration
        {
            AValidator<TConfiguration> validator = null;
            return Result.Try(() => AConfigurationValidator<TConfiguration>.CurrentValidators.TryGetValue(configuration.ValidatorKey, out validator))
                         .Bind(_ => validator == null ? Result.Ok(configuration) : validator.Validate(configuration));
        }

        // convert AConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AConfiguration configuration, JsonSerializerOptions options)
        {
            var specificConfiguration = Convert.ChangeType(configuration, configuration.GetType());
            return JsonSerializer.Serialize(specificConfiguration, options);
        }
    }
}
