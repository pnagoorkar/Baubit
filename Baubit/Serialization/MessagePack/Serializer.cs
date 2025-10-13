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
            try
            {
                obj = (T)MessagePackSerializer.Typeless.Deserialize(bytes, cancellationToken: cancellationToken)!;
            }
            catch (Exception exp)
            {
                obj = default!;
                return false;
            }
            return true;
        }

        public bool TrySerialize<T>(T value, out byte[] serializedValue, CancellationToken cancellationToken = default)
        {
            try
            {
                serializedValue = MessagePackSerializer.Typeless.Serialize(value, cancellationToken: cancellationToken);
            }
            catch (Exception exp)
            {
                serializedValue = [];
                return false;
            }
            return true;
        }
    }
}
