using Baubit.Caching;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.States.DI
{
    public class Module<T> : AModule<Configuration> where T : Enum
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
            services.AddSingleton<StateFactory<T>>(serviceProvider => () => new State<T>(serviceProvider.GetRequiredService<IOrderedCache<T>>(), 
                                                                                         serviceProvider.GetRequiredService<IOrderedCache<StateChanged<T>>>(), 
                                                                                         serviceProvider.GetRequiredService<ILoggerFactory>()));
        }
    }
}
