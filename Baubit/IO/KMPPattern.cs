using FluentResults;

namespace Baubit.IO
{
    public class KMPPattern
    {
        public KMPFrame PrefixFrame { get; init; }
        public KMPFrame SuffixFrame { get; init; }
        public bool Found { get => PrefixFrame.ReachedTheEnd && SuffixFrame.ReachedTheEnd; }

        public KMPPattern(string prefix, string suffix)
        {
            PrefixFrame = new KMPFrame(prefix);
            SuffixFrame = new KMPFrame(suffix);
            SuffixFrame.BeginCaching();
        }

        public void Process(char input)
        {
            if (Found) return;
            if (!PrefixFrame.ReachedTheEnd) PrefixFrame.MoveNext(input);
            else if (!SuffixFrame.ReachedTheEnd) SuffixFrame.MoveNext(input);
            if (Found) ResultTCS.SetResult(Result.Try(SuffixFrame.GetOverflowedCache));
        }

        TaskCompletionSource<Result<string>> ResultTCS = new TaskCompletionSource<Result<string>>();
        public async Task<Result<string>> AwaitResult()
        {
            return await ResultTCS.Task;
        }

        public void Reset()
        {
            PrefixFrame.Reset();
            SuffixFrame.Reset();
            ResultTCS.TrySetCanceled();
            ResultTCS = new TaskCompletionSource<Result<string>>();
        }
    }
}
