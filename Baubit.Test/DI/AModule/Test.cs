
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Traceability;
using FluentResults;

namespace Baubit.Test.DI.AModule
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void ModulesCanBeConstrained(string fileName)
        {
            var configurationSource = new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"] };
            var result = configurationSource.Build().Bind(config => config.Load());
            var reasons = new List<IReason>();
            result.UnwrapReasons(reasons);
            Assert.Contains(reasons, reason => reason is SingularityCheckFailed);
        }
    }
}
