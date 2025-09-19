using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Baubit.Logging.Telemetry.DI
{
    public abstract record AConfiguration : Logging.DI.AConfiguration
    {
        public LoggerConfig Logger { get; init; }
        public MetricsConfig Metrics { get; init; }
        public TracerConfig Tracer { get; init; }
        public ServiceLifetime ActivityMonitorLifetime { get; init; } = ServiceLifetime.Scoped;
        public ActivityTracker.Configuration ActivityTrackerConfiguration { get; init; }

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
            public bool ParentBased { get; init; }
            public string SamplerType { get; init; }
            public double SamplingRatio { get; init; }
        }

    }
}
