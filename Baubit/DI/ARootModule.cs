using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
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
}
