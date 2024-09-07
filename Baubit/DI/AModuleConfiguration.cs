using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Baubit.DI
{
    public abstract class AModuleConfiguration
    {
        public string ConfigurationValidatorKey { get; init; } = "default";
        public string ModuleValidatorKey { get; init; } = "default";
    }

    public static class ModuleConfigurationExtensions
    {
        public static TConfiguration Load<TConfiguration>(this IConfiguration configuration) where TConfiguration : AModuleConfiguration
        {
            // Create the ModuleConfiguration first
            var moduleConfiguration = configuration.Get<TConfiguration>() ??
                                      Activator.CreateInstance<TConfiguration>()!;
            moduleConfiguration.Validate();

            return moduleConfiguration;
        }
        public static TConfiguration Validate<TConfiguration>(this TConfiguration configuration) where TConfiguration : AModuleConfiguration
        {
            if (AModuleConfigurationValidator<TConfiguration>.CurrentValidators.TryGetValue(configuration.ConfigurationValidatorKey, out var validator))
            {
                var validationResult = validator.Validate(configuration);
                if (validationResult != null && !validationResult.IsValid)
                {
                    throw new ValidationException($"Invalid module configuration !{string.Join(Environment.NewLine, validationResult.Errors)}");
                }
            }
            return configuration;
        }

        // convert AModuleConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AModuleConfiguration moduleConfiguration, JsonSerializerOptions options)
        {
            var specificModuleConfiguration = Convert.ChangeType(moduleConfiguration, moduleConfiguration.GetType());
            return JsonSerializer.Serialize(specificModuleConfiguration, options);
        }
    }
}
