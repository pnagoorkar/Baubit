using Baubit.Caching;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.Caching.Setup.DI
{
    public class Module<TValue> : Baubit.Caching.DI.AModule<TValue, Configuration>
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
            services.AddSingleton(serviceProvider => new DummyCache<TValue>(Configuration.CacheConfiguration, serviceProvider.GetRequiredService<ILoggerFactory>()));
            services.AddSingleton<IOrderedCache<TValue>>(serviceProvider => serviceProvider.GetRequiredService<DummyCache<TValue>>());
        }
    }
}
