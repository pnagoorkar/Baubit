﻿using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static IServiceProvider Load(this IConfiguration configuration)
        {
            var rootModule = new RootModule(configuration);
            var services = new ServiceCollection();
            rootModule.Load(services);
            return services.BuildServiceProvider();
        }
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

            ConfigurationSource configurationSource = null;
            IConfiguration iConfiguration = null;

            var configurationSectionGetResult = GetModuleConfigurationSection(configurationSection);
            var configurationSourceSectionGetResult = GetModuleConfigurationSourceSection(configurationSection);

            if (configurationSectionGetResult.IsSuccess) iConfiguration = configurationSectionGetResult.Value;
            if (configurationSourceSectionGetResult.IsSuccess) configurationSource = configurationSourceSectionGetResult.Value.Get<ConfigurationSource>()!;

            iConfiguration = configurationSource.Build(iConfiguration);

            var @object = (T)Activator.CreateInstance(objectType, iConfiguration)!;
            return @object;
        }

        private static Result<IConfigurationSection> GetModuleConfigurationSection(IConfiguration configurationSection)
        {
            var objectConfigurationSection = configurationSection.GetSection("parameters:configuration");
            return objectConfigurationSection.Exists() ? Result.Ok(objectConfigurationSection) : Result.Fail("").WithReason(new ConfigurationNotDefined());
        }

        private static Result<IConfigurationSection> GetModuleConfigurationSourceSection(IConfiguration configurationSection)
        {
            var objectConfigurationSourceSection = configurationSection.GetSection("parameters:configurationSource");
            return objectConfigurationSourceSection.Exists() ? Result.Ok(objectConfigurationSourceSection) : Result.Fail("").WithReason(new ConfigurationSourceNotDefined());
        }

        public static bool TryGetObjectType(this IConfiguration configurationSection, out Type objectType)
        {
            objectType = null;
            var resolutionResult = TypeResolver.TryResolveTypeAsync(configurationSection["type"]!, default).GetAwaiter().GetResult();
            if (resolutionResult.IsSuccess)
            {
                objectType = resolutionResult.Value!;
            }
            return objectType != null;
        }
    }
}
