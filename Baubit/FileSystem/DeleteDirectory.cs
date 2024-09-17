using FluentResults;

namespace Baubit.FileSystem
{
    public static partial class Operations
    {
        public static async Task<Result> DeleteDirectoryAsync(DirectoryDeleteContext context)
        {
            return await Result.Try((Func<Task>)(async () =>
            {
                await Task.Yield();
                if (Directory.Exists(context.Path))
                {
                    Directory.Delete(context.Path, context.Recursive);
                }
            }));
        }
    }

    public class DirectoryDeleteContext
    {
        public string Path { get; init; }
        public bool Recursive { get; init; }
        public DirectoryDeleteContext(string path, bool recursive)
        {
            Path = path;
            Recursive = recursive;
        }
    }
}
