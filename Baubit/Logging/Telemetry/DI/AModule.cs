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
            if (Configuration.Logger.AddConsoleExporter)
            {
                options.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
            foreach (var exporterId in Configuration.Logger.CustomExporters)
            {
                var exporterGetResult = ExporterLookup.TryGetExporter(exporterId);
                if (exporterGetResult.IsSuccess)
                {
                    options.AddProcessor(new SimpleLogRecordExportProcessor(exporterGetResult.Value));
                }
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
            if (Configuration.Metrics.AddConsoleExporter)
            {
                meterProviderBuilder.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
        }

        private void ConfigureTracerProvider(TracerProviderBuilder tracerProviderBuilder)
        {
            tracerProviderBuilder.AddSource(Configuration.Tracer.Sources.ToArray());
            if (Configuration.Tracer.AddConsoleExporter)
            {
                tracerProviderBuilder.AddConsoleExporter(ConfigureOTELConsoleExporterOptions);
            }
        }
    }
}
