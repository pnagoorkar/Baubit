using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.DI
{
    public class Configuration : Baubit.DI.AConfiguration
    {
        public Baubit.Caching.Configuration CacheConfiguration { get; init; }
        public ServiceLifetime CacheLifetime { get; init; } = ServiceLifetime.Singleton;
        public string L1StoreDIKey { get; init; }
        public string L2StoreDIKey { get; init; }
    }
}
