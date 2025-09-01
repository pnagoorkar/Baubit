using Baubit.Traceability.Reasons;

namespace Baubit.Caching.Reasons
{
    public class IdIsNull : AReason
    {
        public static IdIsNull Instance { get; } = new IdIsNull();
    }
}
