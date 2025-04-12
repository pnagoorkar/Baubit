using Baubit.Configuration;
using Baubit.Traceability.Errors;
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
            NestedModules = nestedModules.Concat(GetKnownDependencies()).ToList().AsReadOnly();
            OnInitialized();
        }
        /// <summary>
        /// Called by the constructor in <see cref="AModule"/> after all construction activities.
        /// Override this method to perform construction in child types.
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        /// <summary>
        /// Use this to add any know dependencies to <see cref="NestedModules"/>
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<AModule> GetKnownDependencies() => Enumerable.Empty<AModule>();

        public abstract void Load(IServiceCollection services);
    }

    public abstract class AModule<TConfiguration> : AModule where TConfiguration : AConfiguration
    {
        public new TConfiguration Configuration
        {
            get => (TConfiguration)base.Configuration;
            init => base.Configuration = value;
        }
        protected AModule(ConfigurationSource configurationSource) : this(TryBuildConfigurationSource(configurationSource))
        {

        }

        protected AModule(IConfiguration configuration) : this(TryLoadConfiguration(configuration), TryLoadNestedModules(configuration))
        {

        }
        protected AModule(TConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public override void Load(IServiceCollection services)
        {

        }

        private static IConfiguration TryBuildConfigurationSource(ConfigurationSource configurationSource)
        {
            var buildResult = configurationSource.Build();
            if (!buildResult.IsSuccess)
            {
                throw new AggregateException(new CompositeError<IConfiguration>(buildResult).ToString());
            }
            return buildResult.Value;
        }

        private static TConfiguration TryLoadConfiguration(IConfiguration configuration)
        {
            var loadResult = configuration.Load<TConfiguration>();
            if (!loadResult.IsSuccess)
            {
                throw new AggregateException(new CompositeError<TConfiguration>(loadResult).ToString());
            }
            return loadResult.Value;
        }

        private static List<AModule> TryLoadNestedModules(IConfiguration configuration)
        {
            var loadResult = configuration.GetNestedModules<AModule>();
            if (!loadResult.IsSuccess)
            {
                throw new AggregateException(new CompositeError<List<AModule>>(loadResult).ToString());
            }
            return loadResult.Value;
        }
    }
}
