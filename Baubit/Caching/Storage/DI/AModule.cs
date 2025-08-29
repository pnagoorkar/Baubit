using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.Storage.DI
{
    public abstract class AModule<TConfiguration, TValue> : Baubit.DI.AModule<TConfiguration> where TConfiguration : AConfiguration
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
            services.AddKeyedTransient<IDataStore<TValue>>(Configuration.DIKey, BuildDataStore);
        }

        protected abstract IDataStore<TValue> BuildDataStore(IServiceProvider serviceProvider, object? obj);
    }
}
