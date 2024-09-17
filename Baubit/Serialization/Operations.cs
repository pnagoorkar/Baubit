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
        public static DeserializeXMLFromFile<T> DeserializeXMLFromFile = DeserializeXMLFromFile<T>.GetInstance();
        public static DeserializeXMLString<T> DeserializeXMLString = DeserializeXMLString<T>.GetInstance();
    }
}
