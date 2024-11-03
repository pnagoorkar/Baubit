using FluentResults;

namespace Baubit.Traceability
{
    public static class TraceabilityExtensions
    {
        public static void ToggleTracing<TTRaceable>(this TTRaceable traceable, bool enabled, CancellationToken cancellationToken) where TTRaceable : class, ITraceable
        {
            traceable.ToggleTracing(enabled, cancellationToken);
        }
        public static void CaptureTraceEvent(this object obj, ITraceEvent traceEvent)
        {
            if (obj is ITraceable traceable) traceable.CaptureTraceEvent(traceEvent);
        }

        public static void CaptureTraceEvent(this ITraceable traceable, ITraceEvent traceEvent)
        {
            if (traceable.IsTracingEnabled) traceable.History.Push(traceEvent);
        }

        public static Result Dispose<TDisposable>(this IList<TDisposable> disposables) where TDisposable : IDisposable
        {
            return Result.Try(() =>
            {
                for (int i = 0; i < disposables.Count; i++)
                {
                    disposables[i].Dispose();
                }
            });
        }
    }
}
