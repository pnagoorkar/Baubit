using Baubit.Collections;
using Baubit.Traceability;

namespace Baubit.Test.Traceability.Setup
{
    public class Traceable : ITraceable
    {
        public ObservableConcurrentStack<ITraceEvent> History { get; } = new ObservableConcurrentStack<ITraceEvent>();

        public bool EnableTrace { get; set; }
    }
}
