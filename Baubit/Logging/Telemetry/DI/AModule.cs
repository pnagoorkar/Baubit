using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;

namespace Baubit.Logging.Telemetry.DI
{
    public abstract class AModule<TConfiguration> : Logging.DI.AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        protected AModule(Baubit.Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected AModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected AModule(TConfiguration configuration, List<Baubit.DI.AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        protected override void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            var openTelemetryBuilder = loggingBuilder.Services
                                                     .AddOpenTelemetry();

            if (Configuration.Logger != null)
            {
                openTelemetryBuilder.WithLogging(ConfigureLoggerProviderBuilder, ConfigureOTELLogging);
            }
            if (Configuration.Metrics != null)
            {
                openTelemetryBuilder.WithMetrics(ConfigureMeterProvider);
            }
            if (Configuration.Tracer != null)
            {
                openTelemetryBuilder.WithTracing(ConfigureTracerProvider);
            }
            switch (Configuration.PerfMonitorLifetime)
            {
                case ServiceLifetime.Scoped:
                    loggingBuilder.Services.AddScoped<PerfTracker>(serviceProvider => new PerfTracker(Configuration.PerfTrackerConfiguration, serviceProvider.GetRequiredService<ILoggerFactory>()));
                    break;
                default:
                    loggingBuilder.Services.AddSingleton<PerfTracker>(serviceProvider => new PerfTracker(Configuration.PerfTrackerConfiguration, serviceProvider.GetRequiredService<ILoggerFactory>()));
                    break;
            }

            base.ConfigureLogging(loggingBuilder);
        }

        private void ConfigureLoggerProviderBuilder(LoggerProviderBuilder loggerProviderBuilder)
        {
            //define this method if you want to add .NET specific logging
        }

        private void ConfigureOTELLogging(OpenTelemetryLoggerOptions options)
        {
            options.SetResourceBuilder(ConfigureOTELResourceBuilder());
            AddLoggingExporters(options);
            foreach (var exporterId in Configuration.Logger.CustomExporters)
            {
                var exporterGetResult = ExporterLookup.TryGetExporter(exporterId);
                if (exporterGetResult.IsSuccess)
                {
                    options.AddProcessor(new SimpleLogRecordExportProcessor(exporterGetResult.Value));
                }
            }
        }

        protected virtual void AddLoggingExporters(OpenTelemetryLoggerOptions options)
        {
            if (Configuration.Logger.AddConsoleExporter)
            {
                options.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
        }

        private void ConfigureOTELConsoleExporterOptions(ConsoleExporterOptions options)
        {

        }

        private ResourceBuilder ConfigureOTELResourceBuilder()
        {
            return ResourceBuilder.CreateDefault()
                                  .AddService(Configuration.Logger.ServiceName,
                                              serviceVersion: Configuration.Logger.ServiceVersion);
        }

        private void ConfigureMeterProvider(MeterProviderBuilder meterProviderBuilder)
        {
            meterProviderBuilder.AddMeter(Configuration.Metrics.Meters.ToArray());
            AddMetricsExporters(meterProviderBuilder);
        }

        protected virtual void AddMetricsExporters(MeterProviderBuilder meterProviderBuilder)
        {
            if (Configuration.Metrics.AddConsoleExporter)
            {
                meterProviderBuilder.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
        }

        private void ConfigureTracerProvider(TracerProviderBuilder tracerProviderBuilder)
        {
            tracerProviderBuilder.AddSource(Configuration.Tracer.Sources.ToArray());
            tracerProviderBuilder.SetSampler(GetConfiguredSampler());
            AddTracingExporters(tracerProviderBuilder);
        }

        protected virtual void AddTracingExporters(TracerProviderBuilder tracerProviderBuilder)
        {
            if (Configuration.Tracer.AddConsoleExporter)
            {
                tracerProviderBuilder.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
        }

        /// <summary>
        /// If sampling is parent based, build a sampler such that:
        /// If the current app is responsible for parent span, rootSampler is defined by the Configuration SamplerType
        /// Else current use the sampler defined by the upstream service
        /// If sampling is not parent based, build a sampler as defined by the Configuration SamplerType
        /// </summary>
        /// <returns></returns>
        private Sampler GetConfiguredSampler()
        {
            Func<Sampler> generateRootSampler = () =>
            {
                return Configuration.Tracer.SamplerType switch
                {
                    nameof(AlwaysOnSampler) => new AlwaysOnSampler(),
                    nameof(TraceIdRatioBasedSampler) => new TraceIdRatioBasedSampler(Configuration.Tracer.SamplingRatio),
                    _ => new AlwaysOffSampler()
                };
            };

            return Configuration.Tracer.ParentBased ? new ParentBasedSampler(generateRootSampler()) : generateRootSampler();
        }
    }
}
