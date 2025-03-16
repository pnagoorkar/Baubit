using Baubit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Baubit.DI
{
    public interface IModule
    {
        public AConfiguration Configuration { get; }
        public IReadOnlyList<IModule> NestedModules { get; }
        public void Load(IServiceCollection services);
    }

    public abstract class AModule : IModule
    {
        [JsonIgnore]
        public AConfiguration Configuration { get; init; }
        [JsonIgnore]
        public IReadOnlyList<IModule> NestedModules { get; init; }

        public AModule(AConfiguration configuration, List<AModule> nestedModules)
        {
            Configuration = configuration;
            NestedModules = nestedModules.AsReadOnly();
            OnInitialized();
        }
        /// <summary>
        /// Called by the constructor in <see cref="AModule"/> after all construction activities.
        /// Override this method to perform construction in child types.
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        public abstract void Load(IServiceCollection services);

        //public virtual void Load(IServiceCollection services)
        //{
        //    foreach (var module in NestedModules)
        //    {
        //        module.Load(services);
        //    }
        //}
    }

    public abstract class AModule<TConfiguration> : AModule where TConfiguration : AConfiguration
    {
        public new TConfiguration Configuration
        {
            get => (TConfiguration)base.Configuration;
            init => base.Configuration = value;
        }
        protected AModule(ConfigurationSource configurationSource) : this(configurationSource.Build())
        {

        }

        protected AModule(IConfiguration configuration) : this(configuration.Load<TConfiguration>(),
                                                               configuration.GetNestedModules<AModule>().ToList())
        {

        }
        protected AModule(TConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public override void Load(IServiceCollection services)
        {

        }
    }
}
