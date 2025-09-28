namespace Baubit.Caching
{
    public interface IEntry<TValue>
    {
        public Guid Id { get; }
        public DateTime CreatedOnUTC { get; }
        public TValue Value { get; }
    }
}
