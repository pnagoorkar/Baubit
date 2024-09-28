//using FluentResults;
//using System.Text.Json;

//namespace Baubit.Serialization
//{
//    public static partial class Operations<T>
//    {
//        public static async Task<Result<T>> DeserializeJson(JsonDeserializationContext<T> context)
//        {
//            return await Result.Try((Func<Task<T>>)(async () => 
//            { 
//                await Task.Yield(); 
//                return JsonSerializer.Deserialize<T>(context.JsonString, Operations<T>.IndentedJsonWithCamelCase)!; 
//            }));
//        }
//    }

//    public class JsonDeserializationContext<T>
//    {
//        public string JsonString { get; init; }
//        public JsonDeserializationContext(string jsonString)
//        {
//            JsonString = jsonString;
//        }
//    }
//}
