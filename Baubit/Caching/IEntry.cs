namespace Baubit.Caching
{
    public interface IEntry<TValue>
    {
        public long Id { get; }
        public DateTime CreatedOnUTC { get; }
        public TValue Value { get; }
    }
}
