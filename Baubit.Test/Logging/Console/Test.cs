using Baubit.Configuration;
using Microsoft.Extensions.Logging;
using Baubit.DI;
using Baubit.Traceability;

namespace Baubit.Test.Logging.Console
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanAddConsoleLogger(string fileName)
        {
            var logger = ConfigurationBuilder.CreateNew()
                                                .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Logging.Console.{fileName}"))
                                                .Bind(configBuilder => configBuilder.Build())
                                                .Bind(config => ComponentBuilder<ILogger<Test>>.Create(config))
                                                .Bind(compBuilder => compBuilder.Build()).ThrowIfFailed().Value;

            Assert.NotNull(logger);
        }
    }
}
