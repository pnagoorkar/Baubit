
namespace Baubit.Test.Testing.ScenarioBuilder
{
    public class Test
    {
        [Theory]
        [InlineData("scenario.json")]
        public void CanBuildScenariosWithEmbeddedJsonResources(string fileName)
        {
            var result = Baubit.Testing.ScenarioBuilder<Setup.Scenario>.BuildFromEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Testing.ScenarioBuilder.{fileName}");

            Assert.True(result.IsSuccess);
            Assert.IsType<Setup.Scenario>(result.Value);
        }
    }
}
