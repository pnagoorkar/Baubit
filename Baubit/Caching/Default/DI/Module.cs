using Baubit.Caching.Default;
using Baubit.Caching.DI;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.Default.DI
{
    public class Module<TValue> : AModule<TValue, Configuration>
    {
        public Module(Baubit.Configuration.ConfigurationSource configurationSource) : base(configurationSource)
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
            services.AddSingleton<IOrderedCache<TValue>, InMemoryCache<TValue>>();
        }
    }
}
