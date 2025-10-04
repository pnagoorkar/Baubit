using Baubit.Configuration;
using Baubit.DI;
using Baubit.Traceability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.DI.ComponentBuilder
{
    public class Test
    {
        [Fact]
        public void CanCreateComponentBuilder()
        {
            var createResult = ComponentBuilder<object>.Create();
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Value);
        }
        [Fact]
        public void CanCreateComponentBuilderFromConfigurationSource()
        {
            var createResult = ComponentBuilder<object>.Create(ConfigurationSource.Empty);
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Value);
        }
        [Fact]
        public void CanCreateComponentBuilderFromConfiguration()
        {
            var createResult = ComponentBuilder<object>.Create(ConfigurationSource.Empty.Build().ThrowIfFailed().Value);
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Value);
        }
        [Fact]
        public void CanBuildComponentBuilderWithRegistrationHandler()
        {
            var myObj = new object();
            var createResult = ComponentBuilder<object>.Create()
                                                       .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton(myObj)))
                                                       .Bind(componentBuilder => componentBuilder.Build());
            Assert.True(createResult.IsSuccess);
            Assert.Equal(myObj, createResult.Value);

        }
        [Fact]
        public void CanBuildComponentBuilderWithServiceCollection()
        {
            var myObj = new object();
            var services = new ServiceCollection().AddSingleton(myObj);
            var createResult = ComponentBuilder<object>.Create()
                                                       .Bind(componentBuilder => componentBuilder.WithServiceCollection(services))
                                                       .Bind(componentBuilder => componentBuilder.Build());
            Assert.True(createResult.IsSuccess);
            Assert.Equal(myObj, createResult.Value);

        }
        [Fact]
        public void CanBuildComponentBuilderUsingModules()
        {
            var createResult = ComponentBuilder<ILoggerFactory>.Create()
                                                               .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Logging.DI.Default.Module(new Baubit.Logging.DI.Default.Configuration { AddConsole = true, AddDebug = true }, [], [])))
                                                               .Bind(componentBuilder => componentBuilder.Build());
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Value);

        }
    }
}
