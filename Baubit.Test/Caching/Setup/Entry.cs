using Baubit.Caching;

namespace Baubit.Test.Caching.Setup
{
    public class Entry<TValue> : IEntry<TValue>
    {
        public long Id { get; init; }
        public TValue Value { get; init; }
        public Entry(long id, TValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
