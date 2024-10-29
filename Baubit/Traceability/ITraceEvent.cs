using FluentResults;

namespace Baubit.Traceability
{
    public interface ITraceEvent
    {
        public DateTimeOffset OccurredAt { get; }
        public List<IReason> Reasons { get; }
    }
}
