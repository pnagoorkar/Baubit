using Microsoft.Extensions.Logging;

namespace Baubit.Logging.DI
{
    public abstract class AConfiguration : Baubit.DI.AConfiguration
    {
        public bool AddConsole { get; init; }
        public bool AddDebug { get; init; }
        public bool AddEventSource { get; init; }
        public bool AddEventLog { get; init; }

        public LogLevel ConsoleLogLevel { get; init; } = LogLevel.Information;
        public LogLevel DebugLogLevel { get; init; } = LogLevel.Information;
        public LogLevel EventSourceLogLevel { get; init; } = LogLevel.Information;
        public LogLevel EventLogLogLevel { get; init; } = LogLevel.Information;
    }
}
