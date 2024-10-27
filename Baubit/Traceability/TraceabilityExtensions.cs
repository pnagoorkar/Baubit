namespace Baubit.Traceability
{
    public static class TraceabilityExtensions
    {
        public static void CaptureTraceEventIf(this object obj, bool condition, ITraceEvent traceEvent)
        {
            if (condition && obj is ITraceable traceable) traceable.History.Push(traceEvent);
        }

        public static void CaptureTraceEvent(this object obj, ITraceEvent traceEvent)
        {
            if (obj is ITraceable traceable) traceable.CaptureTraceEvent(traceEvent);
        }

        public static void CaptureTraceEvent(this ITraceable traceable, ITraceEvent traceEvent)
        {
            if (traceable.EnableTrace) traceable.History.Push(traceEvent);
        }
    }
}
