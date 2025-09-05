using Baubit.Configuration;
using Baubit.DI;

namespace Baubit.Aggregation.Fast.Features
{
    public class F000<T> : IFeature
    {
        public IEnumerable<IModule> Modules => 
        [
            new Baubit.Aggregation.Fast.DI.Module<T>(ConfigurationSource.Empty)
        ];
    }
}
