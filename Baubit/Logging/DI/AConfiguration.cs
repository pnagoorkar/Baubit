namespace Baubit.Logging.DI
{
    public abstract class AConfiguration : Baubit.DI.AConfiguration
    {
        public bool AddConsole { get; init; }
        public bool AddDebug { get; init; }
        public bool AddEventSource { get; init; }
        public bool AddEventLog { get; init; }
    }
}
