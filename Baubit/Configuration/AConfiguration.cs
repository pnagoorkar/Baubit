using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Baubit.Configuration
{
    public abstract class AConfiguration
    {
        public string ValidatorKey { get; init; } = "default";
    }

    public static class ConfigurationExtensions
    {
        public static Result<TConfiguration> Load<TConfiguration>(this IConfiguration iConfiguration) where TConfiguration : AConfiguration
        {
            return Result.Try(() => iConfiguration.Get<TConfiguration>() ?? Activator.CreateInstance<TConfiguration>()!)
                         .Bind(config => config.Validate());
        }
        public static Result<TConfiguration> Validate<TConfiguration>(this TConfiguration configuration) where TConfiguration : AConfiguration
        {
            return Result.Try(() =>
            {
                if (AConfigurationValidator<TConfiguration>.CurrentValidators.TryGetValue(configuration.ValidatorKey, out var validator))
                {
                    var validationResult = validator.Validate(configuration);
                    if (validationResult != null && !validationResult.IsValid)
                    {
                        throw new ValidationException($"Invalid configuration !{string.Join(Environment.NewLine, validationResult.Errors)}");
                    }
                }
                return configuration;
            });
        }

        // convert AConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AConfiguration configuration, JsonSerializerOptions options)
        {
            var specificConfiguration = Convert.ChangeType(configuration, configuration.GetType());
            return JsonSerializer.Serialize(specificConfiguration, options);
        }
    }
}
