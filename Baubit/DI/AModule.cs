using Baubit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Baubit.DI
{
    public interface IModule
    {
        public AModuleConfiguration ModuleConfiguration { get; init; }
        public IReadOnlyList<IModule> NestedModules { get; init; }
        public void Load(IServiceCollection services);
    }
    public abstract class AModule : IModule
    {
        [JsonIgnore]
        public AModuleConfiguration ModuleConfiguration { get; init; }
        [JsonIgnore]
        public IReadOnlyList<IModule> NestedModules { get; init; }

        public AModule(AModuleConfiguration moduleConfiguration, List<AModule> nestedModules)
        {
            ModuleConfiguration = moduleConfiguration;
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

        public virtual void Load(IServiceCollection services)
        {
            foreach (var module in NestedModules)
            {
                module.Load(services);
            }
        }
    }

    public abstract class AModule<TConfiguration> : AModule where TConfiguration : AModuleConfiguration
    {
        protected AModule(ConfigurationSource configurationSource) : this(configurationSource.Load())
        {

        }

        protected AModule(IConfiguration configuration) : this(configuration.Load<TConfiguration>(),
                                                               configuration.GetNestedModules<AModule>().ToList())
        {

        }
        protected AModule(TConfiguration moduleConfiguration, List<AModule> nestedModules) : base(moduleConfiguration, nestedModules)
        {
        }
    }
}
