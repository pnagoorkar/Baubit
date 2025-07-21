namespace Baubit.Caching
{
    public interface IEntry<TValue>
    {
        public long Id { get; }
        public DateTime CreatedOnUTC { get; }
        public TValue Value { get; }
    }

    public class Metadata
    {
        public long Id { get; set; }
        public long? Next { get; set; } = null;
        public long? Previous { get; set; } = null;
    }
}
