using Baubit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Baubit.Traceability;
using Baubit.Validation;

namespace Baubit.DI
{
    public interface IModule : IValidatable
    {
        public AConfiguration Configuration { get; }
        public IReadOnlyList<IModule> NestedModules { get; }
        public IReadOnlyList<IConstraint> Constraints { get; }
        public void Load(IServiceCollection services);
    }

    public abstract class AModule : IModule
    {
        [JsonIgnore]
        public AConfiguration Configuration { get; init; }
        [JsonIgnore]
        public IReadOnlyList<IModule> NestedModules { get; init; }

        public IReadOnlyList<IConstraint> Constraints { get; init; }

        public AModule(AConfiguration configuration, List<AModule> nestedModules, List<IConstraint> constraints)
        {
            Configuration = configuration;
            NestedModules = nestedModules.Concat(GetKnownDependencies()).ToList().AsReadOnly();
            Constraints = (constraints ?? new List<IConstraint>()).Concat(GetKnownConstraints()).ToList().AsReadOnly();
            OnInitialized();
        }
        /// <summary>
        /// Called by the constructor in <see cref="AModule"/> after all construction activities.
        /// Override this method to perform construction in child types.
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        protected virtual IEnumerable<IConstraint> GetKnownConstraints() => Enumerable.Empty<IConstraint>();

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

        protected AModule(IConfiguration configuration) : this(TryLoadConfiguration(configuration), 
                                                               TryLoadNestedModules(configuration),
                                                               TryLoadConstraints(configuration))
        {

        }
        protected AModule(TConfiguration configuration, List<AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {

        }

        private static IConfiguration TryBuildConfigurationSource(ConfigurationSource configurationSource)
        {
            return configurationSource.Build().ThrowIfFailed().Value;
        }

        private static TConfiguration TryLoadConfiguration(IConfiguration configuration)
        {
            return configuration.Load<TConfiguration>().ThrowIfFailed().Value;
        }

        private static List<AModule> TryLoadNestedModules(IConfiguration configuration)
        {
            return configuration.LoadModules<AModule>().ThrowIfFailed().Value;
        }

        private static List<IConstraint> TryLoadConstraints(IConfiguration configuration)
        {
            return configuration.LoadConstraints().ThrowIfFailed().Value;
        }
    }
}
