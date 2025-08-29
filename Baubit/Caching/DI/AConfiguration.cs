using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.DI
{
    public abstract class AConfiguration : Baubit.DI.AConfiguration
    {
        public bool IncludeL1Caching { get; init; }
        public int L1MinCap { get; init; } = 128;
        public int L1MaxCap { get; init; } = 8192;
        public Baubit.Caching.Configuration CacheConfiguration { get; init; }
        public ServiceLifetime CacheLifetime { get; init; } = ServiceLifetime.Singleton;
    }
}
