using Baubit.DI;
using Baubit.Caching.InMemory.DI;

namespace Baubit.Caching.InMemory.Features
{
    public class F001<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new DI.Configuration{CacheConfiguration = new Configuration{ RunAdaptiveResizing = true } }, [], [])
        ];
    }
}
