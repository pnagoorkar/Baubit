using FluentResults;

namespace Baubit.FileSystem
{
    public static partial class Operations
    {
        public static async Task<Result> CopyFileAsync(FileCopyContext context)
        {
            return await Result.Try((Func<Task>)(async () =>
            {
                await Task.Yield();
                File.Copy(context.Source, context.Destination, context.Overwrite);
            }));
        }
    }

    public class FileCopyContext
    {
        public string Source { get; init; }
        public string Destination { get; init; }
        public bool Overwrite { get; init; }
        public FileCopyContext(string source, string destination, bool overwrite = false)
        {
            Source = source;
            Destination = destination;
            Overwrite = overwrite;
        }
    }
}
