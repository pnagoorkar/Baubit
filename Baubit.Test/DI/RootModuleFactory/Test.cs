using Baubit.Configuration;
using Baubit.DI;

namespace Baubit.Test.DI.RootModuleFactory
{
    public class Test
    {
        [Theory]
        [InlineData("configWithExplicitRootModule.json")]
        public void CanLoadRootWhenExplicitlyDefined(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.RootModuleFactory.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => Baubit.DI.RootModuleFactory.Create(config));
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<Baubit.Test.DI.RootModuleFactory.Setup.RootModule>(result.Value);
        }
        [Theory]
        [InlineData("configWithOutRootModule.json")]
        public void CanLoadRootWhenNotDefinedAtAll(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.RootModuleFactory.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => Baubit.DI.RootModuleFactory.Create(config));
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<Baubit.DI.RootModule>(result.Value);
        }
        [Theory]
        [InlineData("configWithRootModuleWithModuleConstraints.json")]
        public void CanFailGracefullyWhenRootIsDefinedAndModuleConstraintsFail(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.RootModuleFactory.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => Baubit.DI.RootModuleFactory.Create(config));
            Assert.True(result.IsFailed);
            Assert.Null(result.ValueOrDefault);
        }
        [Theory]
        [InlineData("configWithOutRootModuleWithModuleConstraints.json")]
        public void CanFailGracefullyWhenRootNotDefinedAndModuleConstraintsFail(string fileName)
        {
            var result = ConfigurationBuilder.CreateNew()
                                             .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};DI.RootModuleFactory.{fileName}"))
                                             .Bind(configBuilder => configBuilder.Build())
                                             .Bind(config => Baubit.DI.RootModuleFactory.Create(config));
            Assert.True(result.IsFailed);
            Assert.Null(result.ValueOrDefault);
        }
    }
}
