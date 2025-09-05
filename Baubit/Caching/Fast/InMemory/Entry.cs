namespace Baubit.Caching.Fast.InMemory
{
    public class Entry<TValue> : IEntry<TValue>
    {
        public long Id { get; init; }
        public DateTime CreatedOnUTC { get; init; } = DateTime.UtcNow;
        public TValue Value { get; init; }
        public Entry(long id, TValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
