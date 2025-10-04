using Baubit.Traceability.Reasons;

namespace Baubit.Caching.Reasons
{
    public class FailedToAddEntry<TValue> : AReason
    {
        public FailedToAddEntry(IEntry<TValue> entry) : base("", new Dictionary<string, object> { { nameof(entry), entry } })
        {
            
        }
    }
}
