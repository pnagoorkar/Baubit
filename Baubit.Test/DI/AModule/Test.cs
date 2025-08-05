using Baubit.Configuration;
using Baubit.DI;
using Baubit.DI.Constraints.Reasons;
using Baubit.Traceability;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Baubit.Test.DI.AModule
{
    public class Test
    {
        [Theory]
        [InlineData("configWithModuleConstraints.json")]
        public void ModulesCanBeConstrained(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => ComponentBuilder<object>.Create(config))
                                             .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<object>()))
                                             .Bind(compBuilder => compBuilder.Build());

            var reasons = result.UnwrapReasons().ThrowIfFailed().Value;
            Assert.Contains(reasons, reason => reason is SingularityCheckFailed);
        }

        [Theory]
        [InlineData("configWithModuleHavingDependency.json")]
        public void CanDefineDependenciesViaConstraints(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => ComponentBuilder<object>.Create(config))
                                             .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<object>()))
                                             .Bind(compBuilder => compBuilder.Build());

            var reasons = result.UnwrapReasons().ThrowIfFailed().Value;
            Assert.Contains(reasons, reason => reason is DependencyCheckFailed);

        }

        [Theory]
        [InlineData("configHavingManyModulesIndirectlyDefined.json")]
        public void ModulesCanBeSerialized(string fileName)
        {
            var rootModule = Baubit.DI.RootModuleFactory.Create(new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"] }).Value;

            //var rootModule = new RootModule(new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"] });
            var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var result = rootModule.Serialize(jsonSerializerOptions);

            Assert.True(result.IsSuccess);

            //var reconstructedRoot = new RootModule(new ConfigurationSource { RawJsonStrings = [result.Value] });
            var reconstructedRoot = Baubit.DI.RootModuleFactory.Create(new ConfigurationSource { RawJsonStrings = [result.Value] });
            var reserializationResult = rootModule.Serialize(jsonSerializerOptions);

            Assert.True(reserializationResult.IsSuccess);

            Assert.Equal(result.Value, reserializationResult.Value);
        }

        [Theory]
        [InlineData("configHavingModuleProvidersSection.json")]
        public void CanProvideModulesViaIModuleProvider(string fileName)
        {
            var rootModule = Baubit.DI.RootModuleFactory.Create(new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.AModule.{fileName}"] }).Value;
            Assert.NotNull(rootModule);
            Assert.Single(rootModule.NestedModules);
            Assert.IsType<Baubit.Test.DI.AModule.Setup.Module>(rootModule.NestedModules.First());

        }
    }
}
