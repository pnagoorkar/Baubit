using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Baubit.Test.Logging.Telemetry.ActivityTracker
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void ActivityTrackerWorks(string fileName)
        {
            var configurationSource = new Baubit.Configuration.ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};Logging.Telemetry.ActivityTracker.{fileName}"] };
            var serviceProvider = new ServiceCollection().AddFrom(configurationSource.Build().ValueOrDefault).ValueOrDefault.BuildServiceProvider();
            var activityTracker = serviceProvider.GetRequiredService<Baubit.Logging.Telemetry.ActivityTracker>();

            var exportedItems = new List<Metric>();

            int capturedValue = 0;
            var metricProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("Baubit.Test")
                .AddInMemoryExporter(exportedItems)
                .Build();

            var activity = activityTracker.StartTracking(nameof(ActivityTrackerWorks), System.Diagnostics.ActivityKind.Internal);
            activityTracker.StopTracking(activity);

            metricProvider.ForceFlush();
            Assert.NotEmpty(exportedItems);
            Assert.All(exportedItems, metric => metric.Name.Equals("Baubit.Test"));

        }
    }
}
