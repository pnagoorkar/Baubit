using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching.DI
{
    public class Module<TValue> : Baubit.DI.AModule<Configuration>
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
            switch (Configuration.CacheLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IOrderedCache<TValue>>(BuildOrderedCache);
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<IOrderedCache<TValue>>(BuildOrderedCache);
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<IOrderedCache<TValue>>(BuildOrderedCache);
                    break;
                default: throw new NotImplementedException();
            }
        }

        private IOrderedCache<TValue> BuildOrderedCache(IServiceProvider serviceProvider)
        {
            return new OrderedCache<TValue>(Configuration.CacheConfiguration,
                                            serviceProvider.GetRequiredService<ILoggerFactory>());
            //return new OrderedCache<TValue>(Configuration.CacheConfiguration,
            //                                serviceProvider.GetKeyedService<IDataStore<TValue>>(Configuration.L1StoreDIKey),
            //                                serviceProvider.GetRequiredKeyedService<IDataStore<TValue>>(Configuration.L2StoreDIKey),
            //                                serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
