using Baubit.DI;
using MessagePack;

namespace Baubit.Serialization.MessagePack.DI
{
    public record Configuration : AConfiguration
    {
        public List<IFormatterResolver> FormatResolvers { get; init; }
    }
}
