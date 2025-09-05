using Baubit.DI;
using Baubit.Caching.InMemory.DI;

namespace Baubit.Caching.InMemory.Features
{
    public class F000<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(new InMemory.DI.Configuration{IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100 }, [], [])
        ];
    }
}
