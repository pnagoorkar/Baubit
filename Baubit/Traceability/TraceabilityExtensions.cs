namespace Baubit.Traceability
{
    public static class TraceabilityExtensions
    {
        public static void CaptureTraceEventIf(this object obj, bool condition, ITraceEvent traceEvent)
        {
            if (condition && obj is ITraceable traceable) traceable.History.Push(traceEvent);
        }
    }
}
