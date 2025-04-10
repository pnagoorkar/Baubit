using Baubit.Configuration;
using Baubit.Traceability.Errors;
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
                                                                                                           Action<THostApplicationBuilder, IError> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var registrationResult = hostApplicationBuilder.Configuration
                                                           .GetRootModuleSection()
                                                           .Bind(section => section.TryAs<IRootModule>())
                                                           .Bind(registrar => registrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

            if (!registrationResult.IsSuccess)
            {
                var error = new CompositeError<THostApplicationBuilder>(registrationResult);
                onFailure(hostApplicationBuilder, error);
            }

            return hostApplicationBuilder;
        }

        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
                                                          IError error) where THostApplicationBuilder : IHostApplicationBuilder
        {
            Console.WriteLine(error);
            Environment.Exit(-1);
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
    }
}
