using FluentResults;

namespace Baubit.Traceability
{
    public abstract class ATraceEvent : ITraceEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.Now;
        public List<IReason> Reasons { get; init; } = new List<IReason>();
    }
}
