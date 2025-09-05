using Baubit.Configuration;
using Baubit.DI;

namespace Baubit.Aggregation.Features
{
    public class F000<T> : IFeature
    {
        public IEnumerable<IModule> Modules => 
        [
            new Aggregation.DI.Module<T>(ConfigurationSource.Empty)
        ];
    }
}
