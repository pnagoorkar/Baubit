using Baubit.Caching.Fast.InMemory.DI;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.Fast.InMemory.Features
{
    public class F002<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new Baubit.Caching.Fast.InMemory.DI.Configuration { IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100, CacheLifetime = ServiceLifetime.Transient }, [], [])
        ];
    }
}
