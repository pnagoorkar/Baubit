using Baubit.Caching.DI;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching.InMemory.DI
{
    public class Module<TValue> : AModule<Configuration, TValue>
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
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var l1DataStore = Configuration.IncludeL1Caching ? new DataStore<TValue>(Configuration.L1MinCap, Configuration.L1MaxCap, loggerFactory) : null;
            var l2DataStore = new DataStore<TValue>(loggerFactory);
            var metadata = new Metadata();

            return new OrderedCache<TValue>(Configuration.CacheConfiguration, 
                                            l1DataStore, 
                                            l2DataStore, 
                                            metadata, 
                                            serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
