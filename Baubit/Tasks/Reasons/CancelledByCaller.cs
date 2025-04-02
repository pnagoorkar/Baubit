using Baubit.Traceability.Reasons;

namespace Baubit.Tasks.Reasons
{
    public sealed class CancelledByCaller : AReason
    {
        public CancelledByCaller() : base("Cancelled by caller", default)
        {
        }
    }
}
