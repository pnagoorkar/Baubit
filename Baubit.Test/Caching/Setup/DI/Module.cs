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

        protected override IOrderedCache<TValue> BuildOrderedCache(IServiceProvider serviceProvider)
        {
            return new DummyCache<TValue>(Configuration.CacheConfiguration, serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
