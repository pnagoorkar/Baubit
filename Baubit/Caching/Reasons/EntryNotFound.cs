using Baubit.Traceability.Reasons;

namespace Baubit.Caching.Reasons
{
    public class EntryNotFound<TValue> : AReason
    {
        public long Id { get; init; }
        public EntryNotFound(long id) : base($"Entry ({typeof(TValue).FullName}) with id {id} not found!", [])
        {
            Id = id;
        }
    }
}
