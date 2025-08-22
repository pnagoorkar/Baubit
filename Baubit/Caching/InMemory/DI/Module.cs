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

        protected override IOrderedCache<TValue> BuildOrderedCache(IServiceProvider serviceProvider)
        {
            return new OrderedCache<TValue>(Configuration.CacheConfiguration,
                                            serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
