using Baubit.Configuration;
using Baubit.Configuration.Errors;
using Baubit.Traceability;
using Baubit.Validation.Reasons;
using Xunit.Abstractions;

namespace Baubit.Test.Configuration.AConfiguration
{
    public class Test(ITestOutputHelper testOutputHelper)
    {
        [Theory]
        [InlineData("config.json")]
        public void CanExpandURIs(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsSuccess);
            Assert.Equal("Development", buildResult.Value.CurrentEnvironment);
        }

        [Theory]
        [InlineData("config.json")]
        public void CanDetermineEnvVarNotFound(string fileName)
        {
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{UNKNOWN_ENV_VAR}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsFailed);
            Assert.Contains(buildResult.Reasons, reason => reason is EnvVarNotFound);
        }

        [Theory]
        [InlineData("configWithOutValidator.json")]
        public void CanValidateConfigurationOptionally(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsSuccess);
            Assert.Contains(buildResult.UnwrapReasons().ThrowIfFailed().Value, reason => reason is NoValidatorsDefined);
        }

        [Theory]
        [InlineData("configWithValidator.json")]
        public void CanValidateConfiguration(string fileName)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("BaubitTestAssembly", "Baubit.Test");
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsSuccess);
            Assert.Equal(buildResult.Value.ValidatorTypes.Count, buildResult.UnwrapReasons().ThrowIfFailed().Value.OfType<PassedValidation<Setup.Configuration>>().Count());
        }
    }
}
