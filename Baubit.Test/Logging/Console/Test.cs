using Baubit.Configuration;
using Microsoft.Extensions.Logging;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using FluentResults;

namespace Baubit.Test.Logging.Console
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanAddConsoleLogger(string fileName)
        {
            var configurationSource = new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};Logging.Console.{fileName}"] };

            //var logger = configurationSource.Build().ValueOrDefault.Load().GetRequiredService<ILogger<Test>>();

            var logger = configurationSource.Build()
                                            .Bind(config => config.Load())
                                            .Bind(serviceProvider => Result.Try(() => serviceProvider.GetRequiredService<ILogger<Test>>())).ValueOrDefault;

            Assert.NotNull(logger);
        }
    }
}
