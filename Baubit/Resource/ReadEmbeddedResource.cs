using Baubit.Operation;
using System.Reflection;

namespace Baubit.Resource
{
    public class ReadEmbeddedResource : IOperation<ReadEmbeddedResource.Context, ReadEmbeddedResource.Result>
    {
        private ReadEmbeddedResource()
        {

        }
        private static ReadEmbeddedResource _singletonInstance = new ReadEmbeddedResource();
        public static ReadEmbeddedResource GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                using (Stream stream = context.Assembly.GetManifestResourceStream(context.FullyQualifiedResourceName)!)
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException("Resource not found", context.FullyQualifiedResourceName);
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var str = await reader.ReadToEndAsync();
                        return new Result(true, str);
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
            public string FullyQualifiedResourceName { get; init; }
            public Assembly Assembly { get; init; }

            public Context(string fullyQualifiedResourceName, Assembly assembly)
            {
                FullyQualifiedResourceName = fullyQualifiedResourceName;
                Assembly = assembly;
            }
        }

        public sealed class Result : AResult<string>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, string? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
