namespace Baubit.Traceability
{
    public interface ITraceEvent
    {
        public DateTimeOffset OccurredAt { get; }
    }
}
