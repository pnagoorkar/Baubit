using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Baubit.DI
{
    public sealed class RootModule : ARootModule<RootModuleConfiguration, DefaultServiceProviderFactory, IServiceCollection>
    {
        public RootModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public RootModule(IConfiguration configuration) : base(configuration)
        {
        }

        public RootModule(RootModuleConfiguration moduleConfiguration, List<IModule> nestedModules, List<IConstraint> constraints) : base(moduleConfiguration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            var modules = new List<IModule>();
            this.TryFlatten(modules);
            modules.Remove(this);
            modules.ForEach(module => module.Load(services));
        }

        protected override Action<IServiceCollection> GetConfigureAction() => Load;

        protected override DefaultServiceProviderFactory GetServiceProviderFactory() => new DefaultServiceProviderFactory(Configuration.ServiceProviderOptions);
    }

    public class RootModuleFactory
    {
        public static Result<IRootModule> Create(IConfiguration configuration)
        {
            return configuration.GetRootModuleSectionOrDefault()
                                .Bind(section =>
                                {
                                    if (section == null) //rootModule has not been explicitly set. Fallback to default - Baubit.DI.RootModule
                                    {
                                        var createNewResult = Result.Try(() => new RootModule(configuration));
                                        return (createNewResult.IsSuccess ? Result.Ok<IRootModule>(createNewResult.Value) : Result.Fail(string.Empty)).WithReasons(createNewResult.Reasons);
                                    }
                                    return section.TryAsModule<IRootModule>();
                                });
        }
        public static Result<IRootModule> Create(IConfiguration configuration, params IFeature[] withFeatures)
        {
            if (withFeatures.Any())
            {
                return withFeatures.SelectMany(feature => feature.Modules)
                                   .SerializeAsJsonObject(new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                                   .Bind(jsonString => Configuration.ConfigurationBuilder.CreateNew().Bind(configBuilder => configBuilder.WithRawJsonStrings(jsonString)).Bind(configBuilder => configBuilder.Build()))
                                   .Bind(Create);
            }
            else
            {
                return Create(configuration);
            }
        }
        public static Result<IRootModule> Create(ConfigurationSource configSource)
        {
            return configSource.Build().Bind(Create);
        }
    }
}
