using Baubit.Configuration;

namespace Baubit.Test.Configuration.AConfiguration
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanExpandURIs(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Configuration>());
            Assert.True(buildResult.IsSuccess);
            Assert.Equal("Development", buildResult.Value.CurrentEnvironment);
        }
    }
}
