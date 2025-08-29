namespace Baubit.Caching
{
    public class Configuration
    {
        public bool RunAdaptiveResizing { get; init; } = false;
        public int AdaptionWindowMS { get; init; } = 2_000;
        public int GrowStep { get; init; } = 64;
        public int ShrinkStep { get; init; } = 32;
        public double RoomRateLowerLimit { get; init; } = 1;
        public double RoomRateUpperLimit { get; init; } = 5;
    }
}
