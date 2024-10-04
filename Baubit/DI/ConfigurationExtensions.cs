﻿using Baubit.Configuration;
using Baubit.Store;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static IEnumerable<TModule> GetNestedModules<TModule>(this IConfiguration configuration)
        {
            return configuration.GetSection("modules").GetChildren().Select(section => section.AsAModule<TModule>());
        }

        public static TModule AsAModule<TModule>(this IConfiguration configurationSection)
        {
            if (!configurationSection.TryGetModuleType(out var nestedModuleType))
            {
                throw new ArgumentException("Unable to determine module type !");
            }

            var nestedModuleModuleConfigurationSection = configurationSection.GetSection("parameters:moduleConfiguration");
            var nestedMetaModuleConfigurationSection = configurationSection.GetSection("parameters:configurationSource");

            object nestedModuleConstructionParameter = null;
            if (nestedModuleModuleConfigurationSection.Exists() && nestedMetaModuleConfigurationSection.Exists())
            {
                throw new ArgumentException("Cannot pass ConfigurationSource when ModuleConfiguration is passed and vice versa");
            }
            else if (nestedMetaModuleConfigurationSection.Exists())
            {
                nestedModuleConstructionParameter = nestedMetaModuleConfigurationSection.Get<ConfigurationSource>()!;
            }
            else
            {
                nestedModuleConstructionParameter = nestedModuleModuleConfigurationSection;
            }
            var module = (TModule)Activator.CreateInstance(nestedModuleType, nestedModuleConstructionParameter)!;
            return module;
        }

        public static bool TryGetModuleType(this IConfiguration configurationSection, out Type moduleType)
        {
            moduleType = null;
            var resolutionResult = TypeResolver.ResolveTypeAsync(configurationSection["type"]!, default).GetAwaiter().GetResult();
            if (resolutionResult.IsSuccess)
            {
                moduleType = resolutionResult.Value;
            }
            return moduleType != null;
        }
    }
}
