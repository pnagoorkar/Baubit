using Baubit.Configuration;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public abstract class ARootModule<TConfiguration,
                                      TServiceProviderFactory,
                                      TContainerBuilder> : AModule<TConfiguration>, IRootModule<TContainerBuilder> where TConfiguration : ARootModuleConfiguration
                                                                                                                   where TServiceProviderFactory : IServiceProviderFactory<TContainerBuilder>
                                                                                                                   where TContainerBuilder : notnull
    {
        private IServiceProviderFactory<TContainerBuilder> serviceProviderFactory;
        public IServiceProviderFactory<TContainerBuilder> ServiceProviderFactory
        {
            get
            {
                if (serviceProviderFactory == null)
                {
                    serviceProviderFactory = GetServiceProviderFactory();
                }
                return serviceProviderFactory;
            }
        }

        protected ARootModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected ARootModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected ARootModule(TConfiguration configuration, List<AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        protected override void OnInitialized()
        {
            CheckConstraints();
        }

        protected virtual void CheckConstraints()
        {
            this.TryFlatten().Bind(modules => modules.Remove(this) ? Result.Ok(modules) : Result.Fail(string.Empty))
                             .Bind(modules => modules.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Constraints.CheckAll(modules))))
                             .ThrowIfFailed();
        }

        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(this);
            return hostApplicationBuilder;
        }

        protected abstract TServiceProviderFactory GetServiceProviderFactory();
        protected abstract Action<TContainerBuilder> GetConfigureAction();
        
        public IServiceProvider BuildServiceProvider(IServiceCollection services)
        {
            return CreateServiceProvider(CreateBuilder(services));
        }

        public TContainerBuilder CreateBuilder(IServiceCollection services)
        {
            return ServiceProviderFactory.CreateBuilder(services);
        }

        public IServiceProvider CreateServiceProvider(TContainerBuilder containerBuilder)
        {
            GetConfigureAction()(containerBuilder);
            return ServiceProviderFactory.CreateServiceProvider(containerBuilder);
        }
    }
}
