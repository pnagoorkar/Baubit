using Baubit.Configuration;
using Baubit.DI;
using Baubit.DI.Reasons;
using Baubit.Traceability.Errors;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Baubit.Reflection
{
    public static class ObjectLoader
    {
        /// <summary>
        /// Loads a self contained object into the application domain. <typeparamref name="TSelfContained"/> must be decorated with <see cref="SourceAttribute"/>
        /// </summary>
        /// <typeparam name="TSelfContained">Type of the object to be loaded</typeparam>
        /// <returns>A result that will hold the object in its value if successful </returns>
        public static Result<TSelfContained> Load<TSelfContained>() where TSelfContained : class, ISelfContained
        {
            return Result.Try(() => typeof(TSelfContained).GetCustomAttribute<SourceAttribute>())
                         .Bind(sourceAttribute => sourceAttribute == null ? Result.Fail($"{typeof(TSelfContained).Name}{Environment.NewLine}The generic type parameter TSelfContained requires a {nameof(SourceAttribute)} to be instantiated") : Result.Ok(sourceAttribute))
                         .Bind(sourceAttribute => Result.Try(() => sourceAttribute.ConfigurationSource))
                         .Bind(Load<TSelfContained>);
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource) where TSelfContained : class, ISelfContained
        {
            return Result.Try(() => new ServiceCollection())
                         .Bind(services => services.AddSingleton<TSelfContained>().AddFrom(configSource))
                         .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<TSelfContained>()));
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
