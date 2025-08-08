namespace Baubit.Caching.DI
{
    public class AConfiguration : Baubit.DI.AConfiguration
    {
        public Baubit.Caching.Configuration CacheConfiguration { get; init; }
    }
}
