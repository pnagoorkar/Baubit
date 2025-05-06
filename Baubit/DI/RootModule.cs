using Baubit.Configuration;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        public RootModule(RootModuleConfiguration moduleConfiguration, List<AModule> nestedModules, List<IConstraint> constraints) : base(moduleConfiguration, nestedModules, constraints)
        {
        }

        protected override void OnInitialized()
        {
            this.TryFlatten().Bind(modules => modules.Remove(this) ? Result.Ok(modules) : Result.Fail(string.Empty))
                             .Bind(modules => modules.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Constraints.CheckAll(modules))))
                             .ThrowIfFailed();
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
}
