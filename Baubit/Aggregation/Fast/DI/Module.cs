using Baubit.Caching.Fast;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Baubit.Aggregation.Fast.DI
{
    public class Module<T> : AModule<Configuration>
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
            services.AddSingleton<Aggregator<T>>(BuildAggregator);
            services.AddSingleton<IAggregator<T>>(serviceProvider => serviceProvider.GetRequiredService<Aggregator<T>>());
        }

        private Aggregator<T> BuildAggregator(IServiceProvider serviceProvider)
        {
            return new Aggregator<T>(serviceProvider.GetRequiredService<IOrderedCache<T>>(),
                                     serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
