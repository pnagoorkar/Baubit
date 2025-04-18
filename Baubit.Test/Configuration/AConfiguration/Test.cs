using Baubit.Configuration;
using Baubit.Configuration.Reasons;
using Baubit.Traceability.Errors;
using Baubit.Validation.Reasons;

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
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsSuccess);
            Assert.Equal("Development", buildResult.Value.CurrentEnvironment);
        }

        [Theory]
        [InlineData("config.json")]
        public void CanDetermineEnvVarNotFound(string fileName)
        {
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"${{BaubitTestAssembly}};Configuration.AConfiguration.{fileName}"] };
            var buildResult = configurationSource.Build().Bind(config => config.Load<Setup.Configuration>());
            Assert.True(buildResult.IsFailed);
            Assert.Contains(buildResult.Errors, error => error is CompositeError<string> compErr && compErr.NonErrorReasons.Any(reason => reason is EnvVarNotFound));
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
            Assert.Contains(buildResult.Reasons, reason => reason is ValidatorNotFound);
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
            Assert.Contains(buildResult.Successes, success => success is PassedValidation<Setup.Configuration> pass && pass.ValidationKey.Equals(buildResult.Value.ValidatorKey));
        }
    }
}
