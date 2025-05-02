using Baubit.Configuration;
using Baubit.DI;
using Baubit.DI.Constraints.Reasons;
using Baubit.Traceability;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.DI.AModule
{
    public class Test
    {
        [Theory]
        [InlineData("configWithModuleConstraints.json")]
        public void ModulesCanBeConstrained(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => ComponentBuilder<object>.Create(config))
                                             .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<object>()))
                                             .Bind(compBuilder => compBuilder.Build());

            var reasons = result.UnwrapReasons().ThrowIfFailed().Value;
            Assert.Contains(reasons, reason => reason is SingularityCheckFailed);
        }

        [Theory]
        [InlineData("configWithModuleHavingDependency.json")]
        public void CanDefineDependenciesViaConstraints(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => ComponentBuilder<object>.Create(config))
                                             .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<object>()))
                                             .Bind(compBuilder => compBuilder.Build());

            var reasons = result.UnwrapReasons().ThrowIfFailed().Value;
            Assert.Contains(reasons, reason => reason is DependencyCheckFailed);

        }
    }
}
