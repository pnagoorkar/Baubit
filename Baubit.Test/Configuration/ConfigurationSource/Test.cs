using Baubit.Configuration;

namespace Baubit.Test.Configuration.ConfigurationSource
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanReadConfigurationFromEmbeddedJsonResource(string fileName)
        {
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};Configuration.ConfigurationSource.{fileName}"] };
            var configuration = configurationSource.Build().ValueOrDefault;
            Assert.NotNull(configuration);
            Assert.Equal("value", configuration["key"]);
        }

        [Theory]
        [InlineData("config.json")]
        public void CanExpandURIs(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.ConfigurationSource.{fileName}"] };
            var buildResult = configurationSource.Build();
            Assert.True(buildResult.IsSuccess);
        }
    }
}
