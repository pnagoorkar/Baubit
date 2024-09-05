using Baubit.Operation;
using System.Xml.Serialization;

namespace Baubit.Serialization
{
    public class DeserializeXMLFromFile<T> : IOperation<DeserializeXMLFromFile<T>.Context, DeserializeXMLFromFile<T>.Result>
    {
        private DeserializeXMLFromFile()
        {

        }
        private static DeserializeXMLFromFile<T> _singletonInstance = new DeserializeXMLFromFile<T>();
        public static DeserializeXMLFromFile<T> GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                var fileReadResult = await FileSystem.Operations.ReadFile.RunAsync(new FileSystem.ReadFile.Context(context.Path));
                switch (fileReadResult.Success)
                {
                    default: return new Result(new Exception("", fileReadResult.Exception));
                    case false: return new Result(false, "", fileReadResult);
                    case true:
                        using (StreamReader reader = new StreamReader(fileReadResult.Value!))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(T));
                            var deserializedObject = (T)serializer.Deserialize(reader);
                            return new Result(true, deserializedObject);
                        }
                }
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        public sealed class Context : IContext
        {
            public string Path { get; init; }
            public Context(string path)
            {
                Path = path;
            }
        }

        public sealed class Result : AResult<T>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, T? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
