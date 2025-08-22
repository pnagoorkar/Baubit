using Baubit.Configuration;
using Baubit.DI;

namespace Baubit.Aggregation.Features
{
    /// <summary>
    /// <see cref="Aggregator{T}"/>; No caching features included, No logging features included.<br/>
    /// Consumers MUST include logging and caching features as desired
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class F000<T> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new DI.Module<T>(ConfigurationSource.Empty)
        ];
    }
}
