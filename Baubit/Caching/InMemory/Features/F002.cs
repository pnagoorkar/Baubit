using Baubit.Caching.DI;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.InMemory.Features
{
    public class F002<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new DI.Configuration { CacheConfiguration = new Configuration{ L1StoreInitialCap = 100 }, CacheLifetime = ServiceLifetime.Transient }, [], [])
        ];
    }
}
