namespace Baubit.Traceability
{
    public abstract class ATraceEvent : ITraceEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.Now;
    }
}
