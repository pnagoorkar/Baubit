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
    }
}
