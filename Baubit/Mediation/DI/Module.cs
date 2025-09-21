using Baubit.Aggregation;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Mediation.DI
{
    public class Module : AModule<Configuration>
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
            services.AddSingleton<Mediator>();
            services.AddSingleton<IMediator>(serviceProvider => serviceProvider.GetRequiredService<Mediator>());
            services.AddSingleton<IAggregator>(serviceProvider => serviceProvider.GetRequiredService<IMediator>());
        }
    }
}
