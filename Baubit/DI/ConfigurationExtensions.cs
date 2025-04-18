using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using Baubit.Traceability.Errors;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static Result<IServiceProvider> Load(this IConfiguration configuration)
        {
            return Result.Try(() => new ServiceCollection())
                         .Bind(services => services.AddFrom(configuration))
                         .Bind(services => Result.Try<IServiceProvider>(() => services.BuildServiceProvider()));
        }
        public static Result<IServiceCollection> AddFrom(this IServiceCollection services, IConfiguration configuration)
        {
            return Result.Try(() => new RootModule(configuration))
                         .Bind(rootModule => Result.Try(() => rootModule.Load(services)))
                         .Bind(() => Result.Ok(services));
        }
        public static Result<IServiceCollection> AddFrom(this IServiceCollection services, ConfigurationSource configurationSource) =>  configurationSource.Build().Bind(services.AddFrom);

        public static Result<List<TModule>> GetNestedModules<TModule>(this IConfiguration configuration) where TModule : IModule
        {
            List<TModule> directlyDefinedModules = new List<TModule>();
            List<TModule> indirectlyDefinedModules = new List<TModule>();

            var directlyDefinedModulesExtractionResult = configuration.GetModulesSectionOrDefault()
                                                                      .Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                      .Bind(sections => Result.Merge(sections.Select(section => section.TryAs<TModule>()).ToArray()))
                                                                      .Bind(modules => { directlyDefinedModules = modules.ToList(); return Result.Ok(); });

            var indirectlyDefinedModulesExtractionResult = configuration.GetModuleSourcesSectionOrDefault()
                                                                        .Bind(moduleSourceSection => Result.Try(() => moduleSourceSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                        .Bind(configSections => Result.Merge(configSections.Select(section => section.Get<ConfigurationSource>().Build()).ToArray()))
                                                                        .Bind(configs => Result.Merge(configs.Select(config => config.GetNestedModules<TModule>()).ToArray()))
                                                                        .Bind(modules => { indirectlyDefinedModules = modules.SelectMany(x => x).ToList(); return Result.Ok(); });

            return directlyDefinedModulesExtractionResult.IsSuccess && indirectlyDefinedModulesExtractionResult.IsSuccess ?
                   Result.Ok<List<TModule>>([.. directlyDefinedModules, .. indirectlyDefinedModules]) :
                   Result.Fail(new CompositeError<IEnumerable<TModule>>(directlyDefinedModulesExtractionResult, indirectlyDefinedModulesExtractionResult));
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
                   Result.Fail(new CompositeError<IConfigurationSection>([new ModulesNotDefined()], default, default, default));
        }

        public static Result<IConfigurationSection> GetModuleSourcesSection(this IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection("moduleSources");
            return moduleSourcesSection.Exists() ?
                   Result.Ok(moduleSourcesSection) :
                   Result.Fail(new CompositeError<IConfigurationSection>([new ModuleSourcesNotDefined()], default, default, default));
        }

        public static Result<IConfigurationSection> GetObjectConfigurationSection(this IConfiguration configurationSection)
        {
            var objectConfigurationSection = configurationSection.GetSection("configuration");
            return objectConfigurationSection.Exists() ?
                   Result.Ok(objectConfigurationSection) :
                   Result.Fail(new CompositeError<IConfigurationSection>([new ConfigurationNotDefined()], default, default, default));
        }

        public static Result<IConfigurationSection> GetObjectConfigurationSourceSection(this IConfiguration configurationSection)
        {
            var objectConfigurationSourceSection = configurationSection.GetSection("configurationSource");
            return objectConfigurationSourceSection.Exists() ?
                   Result.Ok(objectConfigurationSourceSection) :
                   Result.Fail(new CompositeError<IConfigurationSection>([new ConfigurationSourceNotDefined()], default, default, default));
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
            return Result.Ok(configuration.GetObjectConfigurationSourceSection().ValueOrDefault?.Get<ConfigurationSource>() ?? new ConfigurationSource());
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
                   Result.Fail(new CompositeError<IConfigurationSection>([new RootModuleNotDefined()], default, default, default));
        }
    }
}
