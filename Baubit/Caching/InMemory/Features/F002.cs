using Baubit.Caching.InMemory.DI;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.InMemory.Features
{
    public class F002<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module<TValue>(InMemory.DI.Configuration.C002, [], [])
        ];
    }
}
