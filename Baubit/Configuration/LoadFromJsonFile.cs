using Baubit.Operation;
using Microsoft.Extensions.Configuration;

namespace Baubit.Configuration
{
    public sealed class LoadFromJsonFile : IOperation<LoadFromJsonFile.Context, LoadFromJsonFile.Result>
    {
        private LoadFromJsonFile()
        {

        }
        private static LoadFromJsonFile _singletonInstance = new LoadFromJsonFile();
        public static LoadFromJsonFile GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile(context.JsonFilePath).Build();
            return new Result(true, configuration);
        }

        public sealed class Context : IContext
        {
            public string JsonFilePath { get; init; }
            public Context(string jsonFilePath)
            {
                JsonFilePath = jsonFilePath;
            }
        }

        public sealed class Result : AResult<IConfiguration>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, IConfiguration? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
