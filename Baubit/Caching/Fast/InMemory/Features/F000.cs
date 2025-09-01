using Baubit.DI;
using Baubit.Caching.Fast.InMemory.DI;

namespace Baubit.Caching.Fast.InMemory.Features
{
    public class F000<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new Baubit.Caching.Fast.InMemory.DI.Configuration{IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100 }, [], [])
        ];
    }
}
