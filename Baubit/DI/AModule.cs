﻿using Baubit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Baubit.Traceability;
using Baubit.Validation;
using System.Linq.Expressions;
using FluentResults;

namespace Baubit.DI
{
    public interface IModule : IValidatable
    {
        public AConfiguration Configuration { get; }
        public IReadOnlyList<IModule> NestedModules { get; }
        public void Load(IServiceCollection services);
        public IEnumerable<Expression<Func<List<IModule>, Result>>> GetConstraints();
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

        public virtual IEnumerable<Expression<Func<List<IModule>, Result>>> GetConstraints() => Enumerable.Empty<Expression<Func<List<IModule>, Result>>>();
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
            return configurationSource.Build().ThrowIfFailed().Value;
        }

        private static TConfiguration TryLoadConfiguration(IConfiguration configuration)
        {
            return configuration.Load<TConfiguration>().ThrowIfFailed().Value;
        }

        private static List<AModule> TryLoadNestedModules(IConfiguration configuration)
        {
            return configuration.GetNestedModules<AModule>().ThrowIfFailed().Value;
        }
    }
}
