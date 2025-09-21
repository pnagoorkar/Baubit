using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Bootstrapping.DI
{
    public class Module<TBootstrapper> : AModule<Configuration> where TBootstrapper : Bootstrapper
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton<TBootstrapper>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<TBootstrapper>());
        }
    }
}
