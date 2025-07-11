namespace Baubit.Caching
{
    public interface IEntry<TValue>
    {
        public long Id { get; }
        public TValue Value { get; }
    }
}
