using Baubit.Traceability.Reasons;

namespace Baubit.Tasks.Reasons
{
    public sealed class TimedOut : AReason
    {
        public TimedOut() : base("Timed out", default)
        {
        }
    }
}
