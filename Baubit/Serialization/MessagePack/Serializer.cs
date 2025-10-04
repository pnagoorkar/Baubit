using MessagePack;

namespace Baubit.Serialization.MessagePack
{
    public class Serializer : ISerializer
    {
        private MessagePackSerializerOptions _messagePackSerializerOptions;
        public Serializer(MessagePackSerializerOptions messagePackSerializerOptions)
        {
            _messagePackSerializerOptions = messagePackSerializerOptions;
        }

        public bool TryDeserialize<T>(byte[] bytes, out T obj, CancellationToken cancellationToken = default)
        {
            obj = (T)MessagePackSerializer.Typeless.Deserialize(bytes, cancellationToken: cancellationToken)!;
            return true;
        }

        public bool TrySerialize<T>(T value, out byte[] serializedValue, CancellationToken cancellationToken = default)
        {
            serializedValue = MessagePackSerializer.Typeless.Serialize(value, cancellationToken: cancellationToken);
            return true;
        }
    }
}
