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
        public static IServiceProvider Load(this IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddFrom(configuration);
            return services.BuildServiceProvider();
        }
        public static IServiceCollection AddFrom(this IServiceCollection services, IConfiguration configuration)
        {
            var rootModule = new RootModule(configuration);
            rootModule.Load(services);
            return services;
        }
        public static IServiceCollection AddFrom(this IServiceCollection services, ConfigurationSource configurationSource) => services.AddFrom(configurationSource.Build());

        public static IEnumerable<TModule> GetNestedModules<TModule>(this IConfiguration configuration)
        {
            var directlyDefinedModules = configuration.GetSection("modules")
                                                      .GetChildren()
                                                      .Select(section => section.As<TModule>());

            var indirectlyDefinedModules = configuration.GetSection("moduleSources")
                                                     .GetChildren()
                                                     .SelectMany(section => section.Get<ConfigurationSource>()
                                                                                   .Build()
                                                                                   .GetNestedModules<TModule>());
            return directlyDefinedModules.Concat(indirectlyDefinedModules);
        }

        public static T As<T>(this IConfiguration configurationSection)
        {
            if (!configurationSection.TryGetObjectType(out var objectType))
            {
                throw new ArgumentException("Unable to determine module type !");
            }

            ConfigurationSource configurationSource = new ConfigurationSource();
            IConfiguration iConfiguration = null;

            var configurationSectionGetResult = GetObjectConfigurationSection(configurationSection);
            var configurationSourceSectionGetResult = GetObjectConfigurationSourceSection(configurationSection);

            if (configurationSectionGetResult.IsSuccess) iConfiguration = configurationSectionGetResult.Value;
            if (configurationSourceSectionGetResult.IsSuccess) configurationSource = configurationSourceSectionGetResult.Value.Get<ConfigurationSource>()!;

            iConfiguration = configurationSource.Build(iConfiguration);

            var @object = (T)Activator.CreateInstance(objectType, iConfiguration)!;
            return @object;
        }

        //public static Result<T> TryAs<T>(this IConfiguration configurationSection)
        //{
        //    if (!configurationSection.TryGetObjectType(out var objectType))
        //    {
        //        throw new ArgumentException("Unable to determine module type !");
        //    }

        //    ConfigurationSource configurationSource = new ConfigurationSource();
        //    IConfiguration iConfiguration = null;

        //    var configurationSectionGetResult = GetObjectConfigurationSection(configurationSection);
        //    var configurationSourceSectionGetResult = GetObjectConfigurationSourceSection(configurationSection);

        //    if (configurationSectionGetResult.IsSuccess) iConfiguration = configurationSectionGetResult.Value;
        //    if (configurationSourceSectionGetResult.IsSuccess) configurationSource = configurationSourceSectionGetResult.Value.Get<ConfigurationSource>()!;

        //    iConfiguration = configurationSource.Build(iConfiguration);

        //    var @object = (T)Activator.CreateInstance(objectType, iConfiguration)!;
        //    return @object;
        //}

        public static Result<T> TryAs<T>(this IConfiguration configuration)
        {
            Type type = null;
            ConfigurationSource objectConfigurationSource = null;

            return TypeResolver.TryResolveTypeAsync(configuration["type"]!)
                               .Bind(typ => { type = typ; return Result.Ok(); })
                               .Bind(configuration.GetObjectConfigurationSourceOrDefault)
                               .Bind(configSource => { objectConfigurationSource = configSource; return Result.Ok(); })
                               .Bind(configuration.GetObjectConfigurationOrDefault)
                               .Bind(config => objectConfigurationSource!.Build2(config))
                               .Bind(config => Result.Try(() => (T)Activator.CreateInstance(type, config)!));
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

        public static Result<ConfigurationSource> GetObjectConfigurationSourceOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSourceSection().ValueOrDefault?.Get<ConfigurationSource>() ?? new ConfigurationSource());
        }

        public static Result<IConfigurationSection> GetObjectConfigurationOrDefault(this IConfiguration configuration)
        {
            return Result.Ok(configuration.GetObjectConfigurationSection().ValueOrDefault);
        }

        public static Result<IConfigurationSection> GetServiceProviderFactorySection(this IConfiguration configurationSection)
        {
            var serviceProviderFactorySection = configurationSection.GetSection("serviceProviderFactory");
            return serviceProviderFactorySection.Exists() ? 
                   Result.Ok(serviceProviderFactorySection) :
                   Result.Fail(new CompositeError<IConfigurationSection>([new ServiceProviderFactorySectionNotDefined()], default, default, default));
        }

        public static bool TryGetObjectType(this IConfiguration configurationSection, out Type objectType)
        {
            objectType = null;
            var resolutionResult = TypeResolver.TryResolveTypeAsync(configurationSection["type"]!);
            if (resolutionResult.IsSuccess)
            {
                objectType = resolutionResult.Value!;
            }
            return objectType != null;
        }
    }
}
