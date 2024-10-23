namespace Baubit.IO
{
    public class KMPResult
    {
        public string Value { get; init; }
        public int IndexInSource { get; init; }
        public KMPTriad ForTriad { get; init; }
        public KMPResult(string value, int indexInSource, KMPTriad forTriad)
        {
            Value = value;
            IndexInSource = indexInSource;
            ForTriad = forTriad;
        }
    }
}
