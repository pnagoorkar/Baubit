using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.DI.RootModuleFactory.Setup
{
    public sealed class RootModule : ARootModule<RootModuleConfiguration, DefaultServiceProviderFactory, IServiceCollection>
    {
        public RootModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public RootModule(IConfiguration configuration) : base(configuration)
        {
        }

        public RootModule(RootModuleConfiguration moduleConfiguration, List<Baubit.DI.IModule> nestedModules, List<IConstraint> constraints) : base(moduleConfiguration, nestedModules, constraints)
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
}
