using Baubit.Configuration;
using Baubit.Store;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static IEnumerable<TModule> GetNestedModules<TModule>(this IConfiguration configuration)
        {
            return configuration.GetSection("modules").GetChildren().Select(section => section.As<TModule>());
        }

        public static T As<T>(this IConfiguration configurationSection)
        {
            if (!configurationSection.TryGetObjectType(out var objectType))
            {
                throw new ArgumentException("Unable to determine module type !");
            }

            var objectConfigurationSection = configurationSection.GetSection("parameters:configuration");
            var objectConfigurationSourceSection = configurationSection.GetSection("parameters:configurationSource");

            object objectConstructionParameter = null;
            if (objectConfigurationSection.Exists() && objectConfigurationSourceSection.Exists())
            {
                throw new ArgumentException("Cannot pass ConfigurationSource when Configuration is passed and vice versa");
            }
            else if (objectConfigurationSourceSection.Exists())
            {
                objectConstructionParameter = objectConfigurationSourceSection.Get<ConfigurationSource>()!;
            }
            else
            {
                objectConstructionParameter = objectConfigurationSection;
            }
            var @object = (T)Activator.CreateInstance(objectType, objectConstructionParameter)!;
            return @object;
        }

        public static bool TryGetObjectType(this IConfiguration configurationSection, out Type objectType)
        {
            objectType = null;
            var resolutionResult = TypeResolver.ResolveTypeAsync(configurationSection["type"]!, default).GetAwaiter().GetResult();
            if (resolutionResult.IsSuccess)
            {
                objectType = resolutionResult.Value;
            }
            return objectType != null;
        }
    }
}
