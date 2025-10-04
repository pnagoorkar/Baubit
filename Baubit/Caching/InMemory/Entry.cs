namespace Baubit.Caching.InMemory
{
    public class Entry<TValue> : IEntry<TValue>
    {
        public Guid Id { get; init; }
        public DateTime CreatedOnUTC { get; init; } = DateTime.UtcNow;
        public TValue Value { get; init; }
        public Entry(Guid id, TValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
