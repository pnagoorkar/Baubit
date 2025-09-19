using Microsoft.Extensions.Logging;

namespace Baubit.Logging.DI.Default
{
    public sealed record Configuration : AConfiguration
    {
        /// <summary>
        /// Default configuration
        /// </summary>
        public static readonly Configuration C000 = new Configuration();

        /// <summary>
        /// <see cref="C000"/> with console logging
        /// </summary>
        public static readonly Configuration C001 = C000 with { AddConsole = true };

        /// <summary>
        /// <see cref="C001"/> with debug logging
        /// </summary>
        public static readonly Configuration C002 = C001 with { AddDebug = true };

        /// <summary>
        /// <see cref="C002"/> with trace enabled
        /// </summary>
        public static readonly Configuration C003 = C002 with { ConsoleLogLevel = LogLevel.Trace, DebugLogLevel = LogLevel.Trace };
    }
}
