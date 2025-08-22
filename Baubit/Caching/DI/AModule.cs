using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.DI
{
    public abstract class AModule<TValue, TConfiguration> : Baubit.DI.AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        protected AModule(Baubit.Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected AModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected AModule(TConfiguration configuration, List<Baubit.DI.IModule> nestedModules, List<Baubit.DI.IConstraint> constraints) : base(configuration, nestedModules, constraints)
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

        protected abstract IOrderedCache<TValue> BuildOrderedCache(IServiceProvider serviceProvider);
    }
}
