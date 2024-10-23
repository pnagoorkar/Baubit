namespace Baubit.IO
{
    public class KMPTriad
    {
        public KMPFrame PrefixFrame { get; init; }
        public KMPFrame SuffixFrame { get; init; }
        public List<KMPResult> KMPResults { get; init; } = new List<KMPResult>();

        private KMPProspect KMPProspect { get; init; }

        public bool Found { get => PrefixFrame.ReachedTheEnd && SuffixFrame.ReachedTheEnd; }

        private int? numOfOccurrences;
        public KMPTriad(string prefix, string suffix, int? numOfOccurrences = null)
        {
            PrefixFrame = new KMPFrame(prefix);
            SuffixFrame = new KMPFrame(suffix);
            KMPProspect = new KMPProspect(SuffixFrame);
            this.numOfOccurrences = numOfOccurrences;
        }

        public void Process(char input, int index)
        {
            if (Found) return;
            if (PrefixFrame.CanMoveForward) 
            {
                PrefixFrame.MoveNext(input);
            }
            else if (SuffixFrame.CanMoveForward)
            {
                SuffixFrame.MoveNext(input);
                KMPProspect.Append(input);
            }
            if (Found)
            {
                KMPResults.Add(new KMPResult(KMPProspect.Value!, index - SuffixFrame.Value.Length - KMPProspect.Value!.Length, this));
                if (numOfOccurrences != null && --numOfOccurrences <= 0)
                {
                    //do nothing
                }
                else
                {
                    Reset();
                }
            }
        }

        public void Reset()
        {
            if (PrefixFrame.Value.Equals(SuffixFrame.Value))
            {
                //Prefix is same as suffix. This means we immediately start considering incoming characters as prospective result.
            }
            else
            {
                PrefixFrame.Reset();
            }
            SuffixFrame.Reset();
            KMPProspect.Reset();
        }
    }
}
