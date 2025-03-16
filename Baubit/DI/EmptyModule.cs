using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public class EmptyModuleConfiguration : AConfiguration
    {

    }
    public sealed class EmptyModule : AModule<EmptyModuleConfiguration>
    {
        public EmptyModule(Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public EmptyModule(IConfiguration configuration) : base(configuration)
        {
        }

        public EmptyModule(EmptyModuleConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }
    }
}
