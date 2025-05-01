using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Collections.DI.ObservableConcurrentList
{
    public class Module<T> : AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton<ObservableConcurrentList<T>>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ObservableConcurrentList<T>>());
            base.Load(services);
        }
    }
}
