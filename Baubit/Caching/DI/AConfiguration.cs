using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.DI
{
    public class AConfiguration : Baubit.DI.AConfiguration
    {
        public Baubit.Caching.Configuration CacheConfiguration { get; init; }
        public ServiceLifetime CacheLifetime { get; init; } = ServiceLifetime.Singleton;
    }
}
