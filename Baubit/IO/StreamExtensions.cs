using FluentResults;
using System.Runtime.CompilerServices;

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
        public static async IAsyncEnumerable<char> EnumerateAsync(this StreamReader streamReader, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            char[] buffer = new char[1];
            while (!cancellationToken.IsCancellationRequested)
            {
                var numRead = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                if (numRead == 0) break;
                yield return buffer[0];
            }
        }

        public static async Task SearchAsync(this StreamReader streamReader,
                                             CancellationToken cancellationToken,
                                             params KMPTriad[] kmpTriads)
        {
            var enumerationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var pendingTriads = kmpTriads.Where(triad => !triad.Found);
            int index = 0;
            await foreach (var currentChar in streamReader.EnumerateAsync(enumerationCancellationTokenSource.Token))
            {
                index++;
                Parallel.ForEach(pendingTriads, kmpTriad => kmpTriad.Process(currentChar, index));
                if (!pendingTriads.Any()) enumerationCancellationTokenSource.Cancel();
            }
        }
    }
}
