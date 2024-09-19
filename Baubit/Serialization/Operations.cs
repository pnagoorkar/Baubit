using System.Text.Json;

namespace Baubit.Serialization
{
    public static partial class Operations<T>
    {
        public static JsonSerializerOptions IndentedJsonWithCamelCase = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
