using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Collections.DI.ObservableConcurrentStack
{
    public class Module<T> : AModule<Configuration>
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
            services.AddSingleton<ObservableConcurrentStack<T>>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ObservableConcurrentStack<T>>());
            base.Load(services);
        }
    }
}
