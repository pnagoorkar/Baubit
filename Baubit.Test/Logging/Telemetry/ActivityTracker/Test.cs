using Baubit.Configuration;
using Baubit.DI;
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Baubit.Traceability;

namespace Baubit.Test.Logging.Telemetry.ActivityTracker
{
    public class Test
    {
        [Theory]
        [InlineData("config.json")]
        public void ActivityTrackerWorks(string fileName)
        {
            var activityTracker = ConfigurationBuilder.CreateNew()
                                                      .Bind(configBuilder => configBuilder.WithEmbeddedJsonResources($"{this.GetType().Assembly.GetName().Name};Logging.Telemetry.ActivityTracker.{fileName}"))
                                                      .Bind(configBuilder => configBuilder.Build())
                                                      .Bind(ComponentBuilder<Baubit.Logging.Telemetry.ActivityTracker>.Create)
                                                      .Bind(compBuilder => compBuilder.Build()).ThrowIfFailed().Value;

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
