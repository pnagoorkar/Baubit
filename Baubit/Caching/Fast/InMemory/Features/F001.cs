using Baubit.DI;
using Baubit.Caching.Fast.InMemory.DI;

namespace Baubit.Caching.Fast.InMemory.Features
{
    public class F001<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new Baubit.Caching.Fast.InMemory.DI.Configuration { IncludeL1Caching = true, L1MinCap = 0, L1MaxCap = 8192, CacheConfiguration = new Configuration{ RunAdaptiveResizing = true } }, [], [])
        ];
    }
}
