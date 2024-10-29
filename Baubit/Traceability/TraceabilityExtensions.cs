namespace Baubit.Traceability
{
    public static class TraceabilityExtensions
    {
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
