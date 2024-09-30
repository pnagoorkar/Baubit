using Baubit.Configuration;
using Baubit.Store;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static IEnumerable<AModule> GetNestedModules(this IConfiguration configuration)
        {
            return configuration.GetSection("modules").GetChildren().Select(section => section.AsAModule());
        }

        public static AModule AsAModule(this IConfigurationSection configurationSection)
        {
            if (!configurationSection.TryGetModuleType(out var nestedModuleType))
            {
                throw new ArgumentException("Unable to determine module type !");
            }

            var nestedModuleModuleConfigurationSection = configurationSection.GetSection("parameters:moduleConfiguration");
            var nestedMetaModuleConfigurationSection = configurationSection.GetSection("parameters:metaConfiguration");

            object nestedModuleConstructionParameter = null;
            if (nestedModuleModuleConfigurationSection.Exists() && nestedMetaModuleConfigurationSection.Exists())
            {
                throw new ArgumentException("Cannot pass MetaConfiguration when ModuleConfiguration is passed and vice versa");
            }
            else if (nestedMetaModuleConfigurationSection.Exists())
            {
                nestedModuleConstructionParameter = nestedMetaModuleConfigurationSection.Get<MetaConfiguration>();
            }
            else
            {
                nestedModuleConstructionParameter = nestedModuleModuleConfigurationSection;
            }
            var module = (AModule)Activator.CreateInstance(nestedModuleType, nestedModuleConstructionParameter)!;
            return module;
        }

        public static bool TryGetModuleType(this IConfigurationSection configurationSection, out Type moduleType)
        {
            moduleType = null;
            var resolutionResult = TypeResolver.ResolveTypeAsync(configurationSection["type"]!).GetAwaiter().GetResult();
            if (resolutionResult.IsSuccess)
            {
                moduleType = resolutionResult.Value;
            }
            return moduleType != null;
        }
    }
}
