using OpenTelemetry.Logs;
using OpenTelemetry;
using System.Text.Json;

namespace Baubit.Logging.Telemetry.Exporters
{
    [Exporter(Id = "be19939a-e8ee-40e9-aea3-567a29175be8")]
    public class JsonConsoleExporter : BaseExporter<LogRecord>
    {
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            using var scope = SuppressInstrumentationScope.Begin();

            foreach (var record in batch)
            {
                var logEntry = new
                {
                    Timestamp = record.Timestamp,
                    Severity = record.LogLevel.ToString(),
                    Category = record.CategoryName,
                    Message = record.Body
                };

                Console.WriteLine(JsonSerializer.Serialize(logEntry));
            }

            return ExportResult.Success;
        }
    }
}
