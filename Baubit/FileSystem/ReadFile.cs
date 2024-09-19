using FluentResults;

namespace Baubit.FileSystem
{
    public static partial class Operations
    {
        public static async Task<Result<string>> ReadFileAsync(FileReadContext context)
        {
            return await Result.Try(() => File.ReadAllTextAsync(context.Path));
        }
    }

    public class FileReadContext 
    {
        public string Path { get; init; }
        public FileReadContext(string path)
        {
            Path = path;
        }
    }
}
