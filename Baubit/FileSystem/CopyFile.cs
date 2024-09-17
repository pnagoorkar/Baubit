using Baubit.Operation;
using FluentResults;

namespace Baubit.FileSystem
{
    public sealed class CopyFile : IOperation<CopyFile.Context, CopyFile.Result>
    {
        private CopyFile()
        {

        }
        private static CopyFile _singletonInstance = new CopyFile();
        public static CopyFile GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(context.Destination)) && context.CreateDestinationFolderIfNotExist)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(context.Destination));
                }
                File.Copy(context.Source, context.Destination, context.Overwrite);
                return new Result(true, null);
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        public sealed class Context : IContext
        {
            public string Source { get; init; }
            public string Destination { get; init; }
            public bool Overwrite { get; init; }
            public bool CreateDestinationFolderIfNotExist { get; init; }
            public Context(string source, string destination, bool overwrite = false, bool createDestinationFolderIfNotExist = true)
            {
                Source = source;
                Destination = destination;
                Overwrite = overwrite;
                CreateDestinationFolderIfNotExist = createDestinationFolderIfNotExist;
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

    public static class CopyFile2
    {
        public static async Task<Result> RunAsync(Context context)
        {
            try
            {
                await Task.Yield();
                File.Copy(context.Source, context.Destination, context.Overwrite);
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
        public class Context
        {
            public string Source { get; init; }
            public string Destination { get; init; }
            public bool Overwrite { get; init; }
            public Context(string source, string destination, bool overwrite = false)
            {
                Source = source;
                Destination = destination;
                Overwrite = overwrite;
            }
        }
    }
}
