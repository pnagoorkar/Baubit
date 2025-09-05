using Baubit.Caching;
using Baubit.Caching.DI;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.Caching.Fast.Setup.DummyL2.DI
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

        protected override IDataStore<TValue> BuildL1DataStore(IServiceProvider serviceProvider)
        {
            return new Baubit.Caching.InMemory.DataStore<TValue>(Configuration.L1MinCap, Configuration.L1MaxCap, serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        protected override IDataStore<TValue> BuildL2DataStore(IServiceProvider serviceProvider)
        {
            return new DummyStore<TValue>(serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        protected override IMetadata BuildMetadata(IServiceProvider serviceProvider)
        {
            return new Baubit.Caching.InMemory.Metadata();
        }
    }
}
