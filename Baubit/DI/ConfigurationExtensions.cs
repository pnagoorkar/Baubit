using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static Result<List<TModule>> LoadModules<TModule>(this IConfiguration configuration) where TModule : class, IModule
        {
            List<TModule> directlyDefinedModules = new List<TModule>();
            List<TModule> indirectlyDefinedModules = new List<TModule>();

            var directlyDefinedModulesExtractionResult = configuration.GetModulesSectionOrDefault()
                                                                      .Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                      .Bind(sections => Result.Merge(sections.Select(section => section.TryAsModule<TModule>()).ToArray()))
                                                                      .Bind(modules => { directlyDefinedModules = modules.ToList(); return Result.Ok(); });

            var indirectlyDefinedModulesExtractionResult = configuration.GetModuleSourcesSectionOrDefault()
                                                                        .Bind(moduleSourceSection => Result.Try(() => moduleSourceSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                        .Bind(configSections => Result.Merge(configSections.Select(section => section.Get<ConfigurationSource>().Build()).ToArray()))
                                                                        .Bind(configs => Result.Merge(configs.Select(config => config.LoadModules<TModule>()).ToArray()))
                                                                        .Bind(modules => { indirectlyDefinedModules = modules.SelectMany(x => x).ToList(); return Result.Ok(); });

            return directlyDefinedModulesExtractionResult.IsSuccess && indirectlyDefinedModulesExtractionResult.IsSuccess ?
                   Result.Ok<List<TModule>>([.. directlyDefinedModules, .. indirectlyDefinedModules]) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReasons(directlyDefinedModulesExtractionResult.Reasons).WithReasons(indirectlyDefinedModulesExtractionResult.Reasons);
        }

        public static Result<TModule> TryAsModule<TModule>(this IConfiguration configuration) where TModule : class, IModule
        {
            return configuration.TryAs<TModule>()
                                .Bind(module => module.TryValidate(module.Configuration.ModuleValidatorTypes));
        }

        public static Result<T> TryAs<T>(this IConfiguration configuration)
        {
            Type type = null;
            ConfigurationSource objectConfigurationSource = null;

            return TypeResolver.TryResolveTypeAsync(configuration["type"]!)
                               .Bind(typ => { type = typ; return Result.Ok(); })
                               .Bind(configuration.GetObjectConfigurationSourceOrDefault)
                               .Bind(configSource => { objectConfigurationSource = configSource; return Result.Ok(); })
                               .Bind(configuration.GetObjectConfigurationOrDefault)
                               .Bind(config => objectConfigurationSource!.Build(config))
                               .Bind(config => Result.Try(() => (T)Activator.CreateInstance(type, config)!));
        }

        public static Result<IConfigurationSection> GetModulesSection(this IConfiguration configurationSection)
        {
            var modulesSection = configurationSection.GetSection("modules");
            return modulesSection.Exists() ?
                   Result.Ok(modulesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModulesNotDefined());
        }

        public static Result<IConfigurationSection> GetModuleSourcesSection(this IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection("moduleSources");
            return moduleSourcesSection.Exists() ?
                   Result.Ok(moduleSourcesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModuleSourcesNotDefined());
        }

        public static Result<IConfigurationSection> GetObjectConfigurationSection(this IConfiguration configurationSection)
        {
            var objectConfigurationSection = configurationSection.GetSection("configuration");
            return objectConfigurationSection.Exists() ?
                   Result.Ok(objectConfigurationSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ConfigurationNotDefined());
        }

        public static Result<IConfigurationSection> GetObjectConfigurationSourceSection(this IConfiguration configurationSection)
        {
            var objectConfigurationSourceSection = configurationSection.GetSection("configurationSource");
            return objectConfigurationSourceSection.Exists() ?
                   Result.Ok(objectConfigurationSourceSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ConfigurationSourceNotDefined());
        }

        public static Result<IConfigurationSection> GetModulesSectionOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetModulesSection().ValueOrDefault);
        }

        public static Result<IConfigurationSection> GetModuleSourcesSectionOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetModuleSourcesSection().ValueOrDefault);
        }

        public static Result<ConfigurationSource> GetObjectConfigurationSourceOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSourceSection().ValueOrDefault?.Get<ConfigurationSource>() ?? ConfigurationSourceBuilder.BuildEmpty().Value);
        }

        public static Result<IConfigurationSection> GetObjectConfigurationOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSection().ValueOrDefault);
        }

        public static Result<IConfigurationSection> GetRootModuleSection(this IConfiguration configurationSection)
        {
            var rootModuleSection = configurationSection.GetSection("rootModule");
            return rootModuleSection.Exists() ?
                   Result.Ok(rootModuleSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new RootModuleNotDefined());
        }
    }
}
