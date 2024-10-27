using Baubit.Collections;

namespace Baubit.Traceability
{
    public interface ITraceable
    {
        public ObservableConcurrentStack<ITraceEvent> History { get; }
        public bool EnableTrace { get; }
    }
}
