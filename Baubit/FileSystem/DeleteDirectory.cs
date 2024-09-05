using Baubit.Operation;
using static Baubit.FileSystem.DeleteDirectory;

namespace Baubit.FileSystem
{
    public sealed class DeleteDirectory : IOperation<Context, Result>
    {
        private DeleteDirectory()
        {

        }
        private static DeleteDirectory _singletonInstance = new DeleteDirectory();
        public static DeleteDirectory GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                Directory.Delete(context.Path, context.Recursive);
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
            public bool Recursive { get; init; }
            public Context(string path, bool recursive)
            {
                Path = path;
                Recursive = recursive;
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
}
