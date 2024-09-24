using FluentResults;

namespace Baubit.IO
{
    public class File
    {
        public static async Task<Result<string>> ReadAllTextAsync(string path)
        {
            return await Result.Try((Func<Task<string>>)(async () => { await Task.Yield(); return System.IO.File.ReadAllText(path); }));
        }
    }
}
