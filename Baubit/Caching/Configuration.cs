namespace Baubit.Caching
{
    public class Configuration
    {
        public int L1StoreInitialCap { get; init; }
        public bool RunAdaptiveResizing { get; init; } = false;
        public int AdaptionWindowMS { get; init; } = 2_000;
        public int GrowStep { get; init; } = 64;
        public int ShrinkStep { get; init; } = 32;
        public int MinCap { get; init; } = 128;
        public int MaxCap { get; init; } = 8192;
        public double GateRateLowerLimit { get; init; } = 1;
        public double GateRateUpperLimit { get; init; } = 5;
    }
}
