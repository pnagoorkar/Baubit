using FluentResults;
using System.Runtime.CompilerServices;
using System.Text;

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

        public static async Task<Result<string>> ReadSubstringAsync(this StreamReader streamReader,
                                                                    string prefix,
                                                                    string suffix,
                                                                    CancellationToken cancellationToken)
        {
            var prefixFrame = new KMPFrame(prefix);
            var suffixFrame = new KMPFrame(suffix);

            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            StringBuilder resultBuilder = new StringBuilder();
            BoundedQueue<char> suffixWindow = new BoundedQueue<char>(suffixFrame.Value.Length);

            suffixWindow.OnDequeue += @char => resultBuilder.Append(@char);

            await foreach (var currentChar in streamReader.EnumerateAsync(linkedCancellationTokenSource.Token))
            {
                if (!prefixFrame.ReachedTheEnd) prefixFrame.MoveNext(currentChar);
                else
                {
                    suffixWindow.Enqueue(currentChar);
                    suffixFrame.MoveNext(currentChar);
                }

                if (suffixFrame.ReachedTheEnd)
                {
                    break;
                }
            }
            linkedCancellationTokenSource.Cancel();
            return Result.Ok(resultBuilder.ToString());
        }

        /// <summary>
        /// Reads substrings that lie between <paramref name="prefix"/> and each of the <paramref name="suffixes"/>
        /// </summary>
        /// <param name="streamReader">The input stream reader</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="prefix">A substring that lies prior to the first substring sought</param>
        /// <param name="suffixes">A set of substrings that fall squentially after each of the sought substring</param>
        /// <returns>A result with substrings found between the <paramref name="prefix"/> and each of the <paramref name="suffixes"/></returns>
        public static async Task<Result<List<string>>> ReadSubstringsAsync(this StreamReader streamReader,
                                                                          CancellationToken cancellationToken,
                                                                          string prefix,
                                                                          params string[] suffixes)
        {
            List<string> results = new List<string>();
            var prefixFrame = new KMPFrame(prefix);
            var suffixFrames = suffixes.Select(suffix => new KMPFrame(suffix)).ToArray();
            var pendingSuffixFrames = suffixFrames.Where(suffix => !suffix.ReachedTheEnd);

            KMPFrame suffixFrame = null!;

            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            StringBuilder resultBuilder = new StringBuilder();

            BoundedQueue<char> suffixWindow = null!;

            Func<bool> setNextSuffix = () =>
            {
                if (suffixFrame != null) //running for the first time. there will be no result yet.
                {
                    results.Add(resultBuilder.ToString());
                    resultBuilder.Clear();
                }

                suffixFrame = pendingSuffixFrames.FirstOrDefault()!;
                if (suffixFrame == null) return false;
                suffixWindow = new BoundedQueue<char>(suffixFrame.Value.Length);
                suffixWindow.OnDequeue += @char => resultBuilder.Append(@char);
                return true;
            };

            setNextSuffix();

            await foreach (var currentChar in streamReader.EnumerateAsync(linkedCancellationTokenSource.Token))
            {
                if (!prefixFrame.ReachedTheEnd) prefixFrame.MoveNext(currentChar);
                else
                {
                    suffixWindow.Enqueue(currentChar);
                    suffixFrame.MoveNext(currentChar);
                }

                if (suffixFrame.ReachedTheEnd && !setNextSuffix())
                {
                    break;
                }
            }
            linkedCancellationTokenSource.Cancel();
            return Result.Ok(results);
        }

        //public static async Task<Result<List<Result<List<string>>>>> ReadSubstringsAsync(this StreamReader streamReader, 
        //                                                                                 CancellationToken cancellationToken,
        //                                                                                 params KMPPatternGroup[] kmpPatternGroups)
        //{
        //    var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        //    await Task.WhenAll(kmpPatternGroups.Select(group => group.AwaitResult())).ContinueWith(task => { linkedCancellationTokenSource.Cancel(); });

        //    await foreach (var currentChar in streamReader.EnumerateAsync(linkedCancellationTokenSource.Token))
        //    {
        //        Parallel.ForEach(kmpPatternGroups, kmpPatternGroup => kmpPatternGroup.Process(currentChar));
        //    }
        //}
    }
}
