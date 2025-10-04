
namespace Baubit.Test.Testing.ScenarioBuilder
{
    public class Test
    {
        [Theory]
        [InlineData("scenario.json")]
        public void CanBuildScenariosWithEmbeddedJsonResources(string fileName)
        {
            var result = Baubit.Testing.ScenarioBuilder<Setup.Context>.CreateFromEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Testing.ScenarioBuilder.{fileName}").Bind(scenarioBuilder => scenarioBuilder.Build());

            Assert.True(result.IsSuccess);
            Assert.IsType<Setup.Scenario>(result.Value);
        }
    }
}
