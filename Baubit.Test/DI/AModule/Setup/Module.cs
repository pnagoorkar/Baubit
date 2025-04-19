using Baubit.Configuration;
using Baubit.DI;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace Baubit.Test.DI.AModule.Setup
{
    public class Module : AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<Baubit.DI.AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public override IEnumerable<Expression<Func<List<IModule>, Result>>> GetConstraints()
        {
            return new SingularityConstraint<Module>().GetConstraints();
        }
    }
}
