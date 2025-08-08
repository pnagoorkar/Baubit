using Baubit.Caching.DI;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Baubit.Caching.InMemory.DI
{
    public class Module<TValue> : AModule<TValue, Configuration>
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
            services.AddSingleton(serviceProvider => new OrderedCache<TValue>(Configuration.CacheConfiguration, serviceProvider.GetRequiredService<ILoggerFactory>()));
            services.AddSingleton<IOrderedCache<TValue>>(serviceProvider => serviceProvider.GetRequiredService<OrderedCache<TValue>>());
        }
    }
}
