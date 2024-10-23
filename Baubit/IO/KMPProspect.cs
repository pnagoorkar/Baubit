using System.Text;

namespace Baubit.IO
{
    public class KMPProspect
    {
        public string? Value { get => suffixFrame.ReachedTheEnd ? cacheOverflow.ToString() : null; }

        BoundedQueue<char> cache;
        StringBuilder cacheOverflow = new StringBuilder();

        KMPFrame suffixFrame;

        public KMPProspect(KMPFrame suffixFrame)
        {
            this.suffixFrame = suffixFrame;
            cache = new BoundedQueue<char>(suffixFrame.Value.Length);
            cache.OnOverflow += @char => cacheOverflow.Append(@char);
        }

        public void Append(char @char)
        {
            cache.Enqueue(@char);
        }

        public void Reset()
        {
            cache.Clear();
            cacheOverflow.Clear();
        }
    }
}
