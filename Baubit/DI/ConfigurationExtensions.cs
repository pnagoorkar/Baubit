using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using Baubit.Traceability;
using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static Result<List<IModule>> LoadModules<TModule>(this IConfiguration configuration)
        {
            List<IModule> featurizedModules = new List<IModule>();
            List<IModule> directlyDefinedModules = new List<IModule>();
            List<IModule> indirectlyDefinedModules = new List<IModule>();

            var directlyProvidedModulesExtractionResult = configuration.GetFeaturesSectionOrDefault()
                                                                       .Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                       .Bind(sections => Result.Merge(sections.AsParallel().Select(sec => GetFeatures(sec.Get<FeatureDescriptor>())).ToArray()))
                                                                       .Bind(features => Result.Try(() => featurizedModules = features.SelectMany(feature => feature.Modules).ToList()))
                                                                       .Bind(_ => Result.Ok());

            var directlyDefinedModulesExtractionResult = configuration.GetModulesSectionOrDefault()
                                                                      .Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                      .Bind(sections => Result.Merge(sections.Select(section => section.TryAsModule<IModule>()).ToArray()))
                                                                      .Bind(modules => { directlyDefinedModules = modules.ToList(); return Result.Ok(); });

            var indirectlyDefinedModulesExtractionResult = configuration.GetModuleSourcesSectionOrDefault()
                                                                        .Bind(moduleSourceSection => Result.Try(() => moduleSourceSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                        .Bind(configSections => Result.Merge(configSections.Select(section => section.Get<ConfigurationSource>().Build()).ToArray()))
                                                                        .Bind(configs => Result.Merge(configs.Select(config => config.LoadModules<TModule>()).ToArray()))
                                                                        .Bind(modules => { indirectlyDefinedModules = modules.SelectMany(x => x).ToList(); return Result.Ok(); });

            return directlyDefinedModulesExtractionResult.IsSuccess && indirectlyDefinedModulesExtractionResult.IsSuccess ?
                   Result.Ok<List<IModule>>([.. featurizedModules,  ..directlyDefinedModules, .. indirectlyDefinedModules]) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReasons(directlyDefinedModulesExtractionResult.Reasons).WithReasons(indirectlyDefinedModulesExtractionResult.Reasons);
        }

        public static Result<IConfiguration> AddModules(this IConfiguration configuration, IEnumerable<IModule> modules)
        {
            if (!modules.Any()) return Result.Ok(configuration);
            return modules.SerializeAsJsonObject(new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                          .Bind(jsonStr => Baubit.Configuration.ConfigurationBuilder.CreateNew().Bind(configBuilder => configBuilder.WithRawJsonStrings(jsonStr)))
                          .Bind(configBuilder => configBuilder.WithAdditionalConfigurations(configuration))
                          .Bind(configBuilder => configBuilder.Build());
        }

        public static Result<List<IConstraint>> LoadConstraints(this IConfiguration configuration)
        {
            return configuration.GetConstraintsSectionOrDefault()
                                .Bind(configSection => Result.Try(() => configSection?.GetChildren()
                                                                                      .Select(constraintSection => constraintSection.TryAs<IConstraint>()
                                                                                                                                   .ThrowIfFailed()
                                                                                                                                   .Value).ToList() ?? new List<IConstraint>()));
        }

        public static Result<IFeature> GetFeatures(FeatureDescriptor featureDescriptor)
        {
            return Result.Try(() => AppDomain.CurrentDomain
                                             .GetAssemblies()
                                             .AsParallel()
                                             .SelectMany(assembly => assembly.GetTypes()
                                                                         .AsParallel()
                                                                         .Where(type => type is not null &&
                                                                                                  type.IsClass &&
                                                                                                  type.IsPublic &&
                                                                                                  !type.IsAbstract &&
                                                                                                  typeof(IFeature).IsAssignableFrom(type) &&
                                                                                                  type.GetCustomAttribute<FeatureIdAttribute>()?.Function == featureDescriptor.Function && 
                                                                                                  type.GetCustomAttribute<FeatureIdAttribute>()?.Variant == featureDescriptor.Variant).ToArray()).SingleOrDefault())
                         .Bind(type => Result.Try(() => (IFeature)Activator.CreateInstance(type)!));
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

            return TypeResolver.TryResolveType(configuration["type"]!)
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

        public static Result<IConfigurationSection> GetConstraintsSectionOrDefault(this IConfiguration configurationSection)
        {
            return Result.Ok(configurationSection.GetConstraintsSection().ValueOrDefault);
        }

        public static Result<IConfigurationSection> GetConstraintsSection(this IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection("moduleConstraints");
            return moduleSourcesSection.Exists() ?
                   Result.Ok(moduleSourcesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ConstraintsNotDefined());
        }

        public static Result<IConfigurationSection> GetModuleSourcesSection(this IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection("moduleSources");
            return moduleSourcesSection.Exists() ?
                   Result.Ok(moduleSourcesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModuleSourcesNotDefined());
        }

        public static Result<IConfigurationSection> GetFeaturesSection(this IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection("features");
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

        public static Result<IConfigurationSection> GetFeaturesSectionOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetFeaturesSection().ValueOrDefault);
        }

        public static Result<ConfigurationSource> GetObjectConfigurationSourceOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSourceSection().ValueOrDefault?.Get<ConfigurationSource>() ?? ConfigurationSourceBuilder.BuildEmpty().Value);
        }

        public static Result<IConfigurationSection> GetObjectConfigurationOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSection().ValueOrDefault);
        }

        public static Result<IConfigurationSection> GetRootModuleSectionOrDefault(this IConfiguration configurationSection)
        {
            return Result.Ok(configurationSection.GetRootModuleSection().ValueOrDefault);
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
