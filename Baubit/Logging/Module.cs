using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;

namespace Baubit.Logging
{
    public sealed class Module : Baubit.Logging.DI.AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }
    }
}
