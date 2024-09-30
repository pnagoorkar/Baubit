using Baubit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public sealed class RootModuleConfiguration : AModuleConfiguration
    {

    }
    public sealed class RootModule : AModule<RootModuleConfiguration>
    {
        public RootModule(MetaConfiguration metaConfiguration) : base(metaConfiguration)
        {
        }

        public RootModule(IConfiguration configuration) : base(configuration)
        {
        }

        public RootModule(RootModuleConfiguration moduleConfiguration, List<AModule> nestedModules) : base(moduleConfiguration, nestedModules)
        {
        }

        public new void Load(IServiceCollection services)
        {
            base.Load(services);
        }
    }
}
