using Baubit.Operation;

namespace Baubit.FileSystem
{
    public class ReadFile : IOperation<ReadFile.Context, ReadFile.Result>
    {
        private ReadFile()
        {

        }
        private static ReadFile _singletonInstance = new ReadFile();
        public static ReadFile GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                var fileContents = await File.ReadAllTextAsync(context.Path);
                return new Result(true, fileContents);
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
