using Baubit.DI;
using Baubit.Caching.InMemory.DI;

namespace Baubit.Caching.InMemory.Features
{
    public class F001<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new InMemory.DI.Configuration { IncludeL1Caching = true, L1MinCap = 0, L1MaxCap = 8192, CacheConfiguration = new Configuration{ RunAdaptiveResizing = true } }, [], [])
        ];
    }
}
