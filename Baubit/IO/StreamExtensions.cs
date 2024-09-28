using FluentResults;

namespace Baubit.IO
{
    public static class StreamExtensions
    {
        public static async Task<Result<string>> ReadStringAsync(this Stream stream)
        {
            try
            {
                if (stream == null) return Result.Fail("Cannot read from a null stream !");
                using (var reader = new StreamReader(stream))
                {
                    var str = await reader.ReadToEndAsync();
                    return Result.Ok(str);
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
        public static async IAsyncEnumerable<char> EnumerateAsync(this StreamReader streamReader)
        {
            char[] buffer = new char[1];
            while (true)
            {
                var numRead = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                if (numRead == 0) break;
                yield return buffer[0];
            }
        }
    }
}
