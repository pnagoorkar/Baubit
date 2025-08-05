using Baubit.Configuration;
using Baubit.DI;
using Baubit.DI.Constraints;
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

        public Module(Configuration configuration, List<Baubit.DI.IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }
    }

    public class MyModuleValidator : AValidator<Module>
    {
        public MyModuleValidator() : base("My module validator")
        {
        }

        protected override IEnumerable<Expression<Func<Module, Result>>> GetRules() => Enumerable.Empty<Expression<Func<Module, Result>>>();
    }

    public class AnotherModuleConfiguration : Baubit.DI.AConfiguration
    {

    }

    public class AnotherModule : Baubit.DI.AModule<AnotherModuleConfiguration>
    {
        public AnotherModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public AnotherModule(IConfiguration configuration) : base(configuration)
        {
        }

        public AnotherModule(AnotherModuleConfiguration configuration, List<Baubit.DI.IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        protected override IEnumerable<IConstraint> GetKnownConstraints()
        {
            return [new Dependency(typeof(Module))];
        }
    }
}
