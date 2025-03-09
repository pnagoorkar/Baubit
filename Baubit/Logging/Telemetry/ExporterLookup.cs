using FluentResults;
using OpenTelemetry.Logs;
using OpenTelemetry;
using System.Reflection;

namespace Baubit.Logging.Telemetry
{
    internal class ExporterLookup
    {
        private static Dictionary<string, BaseExporter<LogRecord>> RegisteredExporters { get; set; } = new Dictionary<string, BaseExporter<LogRecord>>();
        static ExporterLookup()
        {
            RegisteredExporters = AppDomain.CurrentDomain
                                           .GetAssemblies()
                                           .SelectMany(assembly => assembly.GetTypes()
                                                                           .Where(type => type.IsClass &&
                                                                                          type.IsPublic &&
                                                                                          !type.IsAbstract &&
                                                                                          type.IsSubclassOf(typeof(BaseExporter<LogRecord>)) &&
                                                                                          type.CustomAttributes.Any(attribute => attribute.AttributeType.Equals(typeof(ExporterAttribute)))))
                                                                           .Select(type => ((BaseExporter<LogRecord>)Activator.CreateInstance(type)))
                                                                           .ToDictionary(exporter => exporter.GetType().GetCustomAttribute<ExporterAttribute>()!.Id)!;
        }
        internal static Result<BaseExporter<LogRecord>> TryGetExporter(string exporterId)
        {
            BaseExporter<LogRecord> result = null;
            return Result.Try(() => RegisteredExporters.TryGetValue(exporterId, out result))
                         .Bind(getSuccess => getSuccess ? Result.Ok(result) : Result.Fail($"No exporter registered with id: {exporterId}"))!;
        }
    }
}
