using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using FluentResults;
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

            ConfigurationSource configurationSource = null;
            IConfiguration iConfiguration = null;

            object objectConstructionParameter = null;

            var configurationSectionGetResult = GetModuleConfigurationSection(configurationSection);
            var configurationSourceSectionGetResult = GetModuleConfigurationSourceSection(configurationSection);
            var configurationLocalSecretsSectionGetResult = GetModuleConfigurationLocalSecretsSection(configurationSection);

            if (configurationSectionGetResult.IsSuccess && configurationSourceSectionGetResult.IsSuccess)
            {
                throw new ArgumentException("Cannot pass ConfigurationSource when Configuration is passed and vice versa");
            }

            if (configurationSourceSectionGetResult.IsSuccess) configurationSource = configurationSourceSectionGetResult.Value.Get<ConfigurationSource>()!;

            if (configurationSectionGetResult.IsSuccess) iConfiguration = configurationSectionGetResult.Value;

            if (configurationLocalSecretsSectionGetResult.IsSuccess)
            {
                if (configurationSource != null)
                {
                    configurationSource.LocalSecrets.AddRange(configurationLocalSecretsSectionGetResult.Value.GetChildren().Select(sec => sec.Value!));
                }
                else
                {
                    var configurationBuilder = new ConfigurationBuilder();
                    foreach (var secretsIdConfigSection in configurationLocalSecretsSectionGetResult.Value.GetChildren())
                    {
                        configurationBuilder.AddUserSecrets(secretsIdConfigSection.Value);
                    }
                    if (iConfiguration != null)
                    {
                        configurationBuilder.AddConfiguration(iConfiguration);
                    }
                    iConfiguration = configurationBuilder.Build();
                }
            }

            objectConstructionParameter = configurationSource != null ? configurationSource : iConfiguration;

            var objectConfigurationSection = configurationSection.GetSection("parameters:configuration");
            var objectConfigurationSourceSection = configurationSection.GetSection("parameters:configurationSource");

            var localSecretsConfigurationSection = configurationSection.GetSection("parameters:localSecrets");

            //object objectConstructionParameter = null;
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

        private static Result<IConfigurationSection> GetModuleConfigurationLocalSecretsSection(IConfiguration configurationSection)
        {
            var localSecretsConfigurationSection = configurationSection.GetSection("parameters:localSecrets");
            return localSecretsConfigurationSection.Exists() ? Result.Ok(localSecretsConfigurationSection) : Result.Fail("").WithReason(new LocalSecretsNotDefined());
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
