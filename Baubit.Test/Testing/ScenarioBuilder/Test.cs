
namespace Baubit.Test.Testing.ScenarioBuilder
{
    public class Test
    {
        [Theory]
        [InlineData("scenario.json")]
        public void CanBuildScenariosWithEmbeddedJsonResources(string fileName)
        {
            var scenario = Baubit.Testing.ScenarioBuilder<Setup.Scenario, Setup.Context>.Create()
                                                                                        .Bind(bldr => bldr.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Testing.ScenarioBuilder.{fileName}"))
                                                                                        .Bind(bldr => bldr.Build());

            Assert.True(scenario.IsSuccess);
            Assert.IsType<Setup.Scenario>(scenario.Value);
        }
    }
}
