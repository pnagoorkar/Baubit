using System.Diagnostics.Metrics;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Baubit.Logging.Telemetry
{
    public sealed class PerfTracker
    {
        private readonly Meter _meter;
        private readonly Counter<int> _requestCounter;
        private readonly ObservableGauge<long> _activeRequests;
        private readonly Histogram<double> _requestDuration;
        private long _activeRequestCount = 0;
        private readonly ActivitySource _activitySource;

        private readonly ILoggerFactory _loggerFactory; //use logger factory to log anything from this class. This is to avoid recursive logging
        public PerfTracker(Configuration configuration, ILoggerFactory loggerFactory)
        {
            _meter = new Meter(configuration.MeterConfig.ServiceName, configuration.MeterConfig.ServiceVersion);
            _activitySource = new ActivitySource(configuration.MeterConfig.ServiceName);

            // Define performance metrics
            _requestCounter = _meter.CreateCounter<int>(configuration.CounterConfig.Label);
            _activeRequests = _meter.CreateObservableGauge(configuration.GaugeConfig.Label, () => Interlocked.Read(ref _activeRequestCount));
            _requestDuration = _meter.CreateHistogram<double>(configuration.HistogramConfig.Label, configuration.HistogramConfig.Unit);
            _loggerFactory = loggerFactory;
        }

        public Activity? StartTracking(string operationName, ActivityKind kind)
        {
            Interlocked.Increment(ref _activeRequestCount);
            _requestCounter.Add(1);

            // Start distributed tracing
            return _activitySource.StartActivity(operationName, kind);
        }

        public void StopTracking(Activity? activity)
        {
            if (activity != null)
            {
                Interlocked.Decrement(ref _activeRequestCount);
                _requestDuration.Record(activity.Duration.TotalMilliseconds);
                activity.Stop();
            }
        }

        public class Configuration
        {
            public Meter MeterConfig { get; init; }
            public Counter CounterConfig { get; init; }
            public Gauge GaugeConfig { get; init; }
            public Histogram HistogramConfig { get; init; }

            public class Meter
            {
                public string ServiceName { get; init; }
                public string ServiceVersion { get; init; }
            }

            public class Counter
            {
                public string Label { get; init; }
            }

            public class Gauge
            {
                public string Label { get; init; }
            }

            public class Histogram
            {
                public string Label { get; init; }
                public string Unit { get; init; }
            }
        }
    }
}
