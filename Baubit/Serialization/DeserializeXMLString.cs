using Baubit.Operation;
using System.Xml.Serialization;

namespace Baubit.Serialization
{
    public class DeserializeXMLString<T> : IOperation<DeserializeXMLString<T>.Context, DeserializeXMLString<T>.Result>
    {
        private DeserializeXMLString()
        {

        }
        private static DeserializeXMLString<T> _singletonInstance = new DeserializeXMLString<T>();
        public static DeserializeXMLString<T> GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                using (StreamReader reader = new StreamReader(context.XMLString))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    var deserializedObject = (T)serializer.Deserialize(reader);
                    return new Result(true, deserializedObject);
                }
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        public sealed class Context : IContext
        {
            public string XMLString { get; init; }
            public Context(string xmlString)
            {
                this.XMLString = xmlString;
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
