using Baubit.Configuration;
using Baubit.DI;
using Baubit.Validation;
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

        public Module(Configuration configuration, List<Baubit.DI.AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        protected override IEnumerable<IConstraint> GetKnownConstraints()
        {
            return [new SingularityConstraint<Module>()];
        }
    }

    public class MyModuleValidator : AValidator<Module>
    {
        public MyModuleValidator() : base("My module validator")
        {
        }

        protected override IEnumerable<Expression<Func<Module, Result>>> GetRules() => Enumerable.Empty<Expression<Func<Module, Result>>>();
    }
}
