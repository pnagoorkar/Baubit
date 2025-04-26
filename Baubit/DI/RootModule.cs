using Baubit.Configuration;
using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IRootModule : IModule
    {
        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
    }

    public abstract class ARootModuleConfiguration : AConfiguration
    {
        public bool DisableConstraints { get; init; }
    }
    public sealed class RootModuleConfiguration : ARootModuleConfiguration
    {
        public ServiceProviderOptions ServiceProviderOptions { get; init; } = new ServiceProviderOptions();
    }

    public abstract class ARootModule<TConfiguration, 
                                      TServiceProviderFactory, 
                                      TContainerBuilder> : AModule<TConfiguration>, IRootModule where TConfiguration : ARootModuleConfiguration 
                                                                                                  where TServiceProviderFactory : IServiceProviderFactory<TContainerBuilder>
                                                                                                  where TContainerBuilder : notnull
    {
        protected ARootModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected ARootModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected ARootModule(TConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(GetServiceProviderFactory(), GetConfigureAction());
            return hostApplicationBuilder;
        }

        protected abstract TServiceProviderFactory GetServiceProviderFactory();
        protected abstract Action<TContainerBuilder> GetConfigureAction();

    }
    public sealed class RootModule : ARootModule<RootModuleConfiguration, DefaultServiceProviderFactory, IServiceCollection>
    {
        public RootModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public RootModule(IConfiguration configuration) : base(configuration)
        {
        }

        public RootModule(RootModuleConfiguration moduleConfiguration, List<AModule> nestedModules) : base(moduleConfiguration, nestedModules)
        {
        }

        protected override void OnInitialized()
        {
            if (!Configuration.DisableConstraints)
            {
                Configuration.ModuleValidatorKeys.Add(typeof(RootValidator<RootModule>).AssemblyQualifiedName);
            }
        }

        public override void Load(IServiceCollection services)
        {
            var modules = new List<IModule>();
            this.TryFlatten(modules);
            modules.Remove(this);
            modules.ForEach(module => module.Load(services));
        }

        protected override Action<IServiceCollection> GetConfigureAction() => Load;

        protected override DefaultServiceProviderFactory GetServiceProviderFactory() => new DefaultServiceProviderFactory(Configuration.ServiceProviderOptions);
    }

    public static class ModuleExtensions
    {
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var registrationResult = hostApplicationBuilder.Configuration
                                                           .GetRootModuleSection()
                                                           .Bind(section => section.TryAsModule<IRootModule>())
                                                           .Bind(registrar => registrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

            if (registrationResult.IsFailed)
            {
                onFailure(hostApplicationBuilder, registrationResult);
            }

            return hostApplicationBuilder;
        }

        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
                                                          IResultBase result) where THostApplicationBuilder : IHostApplicationBuilder
        {
            Console.WriteLine(result.ToString());
            Environment.Exit(-1);
        }
        public static Result<List<IModule>> TryFlatten<TModule>(this TModule module) where TModule : IModule
        {
            return Result.Try(() => new List<IModule>())
                         .Bind(modules => module.TryFlatten(modules) ? Result.Ok(modules) : Result.Fail(""));
        }
        public static bool TryFlatten<TModule>(this TModule module, List<IModule> modules) where TModule : IModule
        {
            if (modules == null) modules = new List<IModule>();

            modules.Add(module);

            foreach (var nestedModule in module.NestedModules)
            {
                nestedModule.TryFlatten(modules);
            }

            return true;
        }

        public static Result CheckConstraints<TRootModule>(this TRootModule rootModule) where TRootModule : IRootModule
        {
            return rootModule.TryFlatten()
                             .Bind(modules => modules.Remove(rootModule) ? Result.Ok(modules) : Result.Fail(string.Empty))
                             .Bind(modules => modules.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.CheckConstraints(modules))));
        }

        public static Result CheckConstraints<TModule>(this TModule module, List<IModule> modules) where TModule : class, IModule
        {
            return module.TryValidate(module.Configuration.ModuleValidatorTypes, modules.Cast<IConstrainable>().ToList()).Bind(m => Result.Ok());
        }
    }
}
