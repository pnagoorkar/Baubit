using Baubit.Configuration;
using Baubit.Traceability;

namespace Baubit.Test.Configuration.ConfigurationSource
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanReadConfigurationFromEmbeddedJsonResource(string fileName)
        {
            var configuration =  ConfigurationBuilder.CreateNew()
                                                     .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Configuration.ConfigurationSource.{fileName}"))
                                                     .Bind(configBuilder => configBuilder.Build()).ThrowIfFailed().Value;
            Assert.NotNull(configuration);
            Assert.Equal("value", configuration["key"]);
        }

        [Theory]
        [InlineData("config.json")]
        public void CanExpandURIs(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");

            var buildResult = ConfigurationBuilder.CreateNew()
                                                     .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Configuration.ConfigurationSource.{fileName}"))
                                                     .Bind(configBuilder => configBuilder.Build());
            Assert.True(buildResult.IsSuccess);
        }
    }
}
