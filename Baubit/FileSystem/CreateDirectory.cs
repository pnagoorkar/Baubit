using FluentResults;

namespace Baubit.FileSystem
{
    public static partial class Operations
    {
        public static async Task<Result> CreateDirectoryAsync(DirectoryCreateContext context)
        {
            return await Result.Try((Func<Task>)(async () =>
            {
                await Task.Yield();
                Directory.CreateDirectory(context.Path);
            }));
        }
    }

    public class DirectoryCreateContext
    {
        public string Path { get; init; }
        public DirectoryCreateContext(string path)
        {
            Path = path;
        }
    }
}
