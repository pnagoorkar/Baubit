using FluentResults;
using System.Text;

namespace Baubit.IO
{
    public class KMPPatternGroup
    {
        public KMPFrame Prefix { get; init; }
        public KMPFrame[] Suffixes { get; init; }
        public KMPFrame? CurrentSuffix { get; private set; }

        public bool SearchComplete { get => Prefix.ReachedTheEnd && CurrentSuffix == null; }

        List<string> results = new List<string>();
        StringBuilder resultBuilder = new StringBuilder();
        BoundedQueue<char> suffixWindow = null;

        public KMPPatternGroup(string prefix, params string[] suffixes)
        {
            Prefix = new KMPFrame(prefix);
            Suffixes = suffixes.Select(suffix => new KMPFrame(suffix)).ToArray();
            MoveNext();
        }

        private bool MoveNext()
        {
            if (CurrentSuffix == null)
            {
                results.Add(resultBuilder.ToString());
                resultBuilder.Clear();
            }

            CurrentSuffix = Suffixes.FirstOrDefault(suffix => !suffix.ReachedTheEnd)!;
            if (CurrentSuffix == null) return false;

            suffixWindow = new BoundedQueue<char>(CurrentSuffix.Value.Length);
            suffixWindow.OnDequeue += @char => resultBuilder.Append(@char);
            return true;
        }

        public void Process(char next)
        {
            if (SearchComplete) return;
            if (!Prefix.ReachedTheEnd) Prefix.MoveNext(next);
            else
            {
                suffixWindow.Enqueue(next);
                CurrentSuffix!.MoveNext(next);
            }
            if (CurrentSuffix!.ReachedTheEnd && !MoveNext())
            {
                ResultTCS.SetResult(Result.Ok(results));
            }
        }

        private TaskCompletionSource<Result<List<string>>> ResultTCS = new TaskCompletionSource<Result<List<string>>>();
        public async Task<Result<List<string>>> AwaitResult()
        {
            return await ResultTCS.Task;
        }
    }
}
