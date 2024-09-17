using Baubit.Operation;
using FluentResults;

namespace Baubit.FileSystem
{
    public class CreateDirectory : IOperation<CreateDirectory.Context, CreateDirectory.Result>
    {
        private CreateDirectory()
        {
            
        }
        private static CreateDirectory _singletonInstance = new CreateDirectory();
        public static CreateDirectory GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                Directory.CreateDirectory(context.Path);
                return new Result(true, null);
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

        public sealed class Result : AResult
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, object? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }

    public static class CreateDirectory2
    {
        public static async Task<Result> RunAsync(Context context)
        {
            try
            {
                await Task.Yield();
                Directory.CreateDirectory(context.Path);
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        public sealed class Context
        {
            public string Path { get; init; }
            public Context(string path)
            {
                Path = path;
            }
        }
    }
}
