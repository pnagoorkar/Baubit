namespace Baubit.Serialization.MessagePack
{
    public interface ISerializer : Baubit.Serialization.ISerializer
    {
        public bool TrySerialize<T>(T value, out byte[] serializedValue, CancellationToken cancellationToken = default);
        public bool TryDeserialize<T>(byte[] bytes, out T obj, CancellationToken cancellationToken = default);
    }
}
