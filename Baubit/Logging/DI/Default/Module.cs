using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;

namespace Baubit.Logging.DI.Default
{
    public sealed class Module : AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }
    }
}
