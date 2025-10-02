using MessagePack;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Baubit.Caching.Redis
{
    [MessagePackObject]
    public class Entry<TValue> : IEntry<TValue>
    {
        [Key(0)]
        public Guid Id { get; init; }

        [Key(1)]
        public TValue Value { get; init; }

        private DateTime _createdOnUTC = DateTime.UtcNow;

        [Key(2)]
        public DateTime CreatedOnUTC { get => _createdOnUTC; private set => _createdOnUTC = value; }

        [SerializationConstructor]
        public Entry(Guid id, TValue value, DateTime createdOn = default)
        {
            Id = id;
            Value = value;
            if (createdOn != default) CreatedOnUTC = createdOn;
        }

        //[Obsolete("For use with serialization only", error: true)]
        //[SerializationConstructor]
        //public Entry()
        //{

        //}
    }
}
