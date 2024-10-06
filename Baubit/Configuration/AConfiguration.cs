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
        public static TConfiguration Load<TConfiguration>(this IConfiguration iConfiguration) where TConfiguration : AConfiguration
        {
            // Create the configuration first
            var configuration = iConfiguration.Get<TConfiguration>() ??
                                      Activator.CreateInstance<TConfiguration>()!;
            configuration.Validate();

            return configuration;
        }
        public static TConfiguration Validate<TConfiguration>(this TConfiguration configuration) where TConfiguration : AConfiguration
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
        }

        // convert AConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AConfiguration configuration, JsonSerializerOptions options)
        {
            var specificConfiguration = Convert.ChangeType(configuration, configuration.GetType());
            return JsonSerializer.Serialize(specificConfiguration, options);
        }
    }
}
