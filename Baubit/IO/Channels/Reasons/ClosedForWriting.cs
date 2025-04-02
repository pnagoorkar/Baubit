using Baubit.Traceability.Reasons;

namespace Baubit.IO.Channels.Reasons
{
    public sealed class ClosedForWriting : AReason
    {
        public ClosedForWriting() : base("Channel closed for writing", default)
        {
        }
    }
}
