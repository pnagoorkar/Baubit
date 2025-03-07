using Baubit.Configuration;
using Microsoft.Extensions.Logging;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.Logging.Console
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void CanAddConsoleLogger(string fileName)
        {
            var configurationSource = new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};Logging.Console.{fileName}"] };

            var logger = configurationSource.Build().Load().GetRequiredService<ILogger<Test>>();

            Assert.NotNull(logger);
        }
    }
}
