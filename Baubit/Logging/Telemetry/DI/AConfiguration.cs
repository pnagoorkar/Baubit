using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Baubit.Logging.Telemetry.DI
{
    public abstract class AConfiguration : Logging.DI.AConfiguration
    {
        public LoggerConfig Logger { get; init; }
        public MetricsConfig Metrics { get; init; }
        public TracerConfig Tracer { get; init; }
        public ServiceLifetime PerfMonitorLifetime { get; init; } = ServiceLifetime.Scoped;
        public PerfTracker.Configuration PerfTrackerConfiguration { get; init; }

        public class LoggerConfig
        {
            public bool AddConsoleExporter { get; init; }
            public string ServiceName { get; init; }
            public string ServiceVersion { get; init; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
            public List<string> CustomExporters { get; init; } = new List<string>();
        }

        public class MetricsConfig
        {
            public bool AddConsoleExporter { get; init; }
            public List<string> Meters { get; init; } = new List<string>();
            public List<string> CustomExporters { get; init; } = new List<string>();
        }

        public class TracerConfig
        {
            public bool AddConsoleExporter { get; init; }
            public List<string> Sources { get; init; } = new List<string>();
            public List<string> CustomExporters { get; init; } = new List<string>();
        }
    }
}
