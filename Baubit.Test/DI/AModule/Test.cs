using Baubit.Configuration;
using Baubit.DI;
using Baubit.Traceability;
using Baubit.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.DI.AModule
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void ModulesCanBeConstrained(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => ComponentBuilder<object>.Create(config))
                                             .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<object>()))
                                             .Bind(compBuilder => compBuilder.Build(true));

            var reasons = result.UnwrapReasons().ThrowIfFailed().Value;
            Assert.Contains(reasons, reason => reason is SingularityCheckFailed);
        }
    }
}
