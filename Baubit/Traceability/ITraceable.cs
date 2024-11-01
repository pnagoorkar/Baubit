using Baubit.Collections;
using Baubit.Hosting;

namespace Baubit.Traceability
{
    public interface ITraceable
    {
        public ObservableConcurrentStack<ITraceEvent> History { get; }
        public bool IsTracingEnabled { get => History?.ObservationEnabled ?? false; }

        public void ToggleTracing(bool enabled, CancellationToken cancellationToken) => History.SwitchState(enabled, cancellationToken);
    }
}
