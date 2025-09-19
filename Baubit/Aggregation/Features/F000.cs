using Baubit.Aggregation.DI;
using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;

namespace Baubit.Aggregation.Features
{
    public class F000<T> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Aggregation.DI.Module<T>(Baubit.Aggregation.DI.Configuration.C000, [], [])
        ];
    }
}
