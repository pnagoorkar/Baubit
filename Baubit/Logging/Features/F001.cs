using Baubit.DI;
using Baubit.Logging.DI.Default;

namespace Baubit.Logging.Features
{
    /// <summary>
    /// Console and Debug logging only<br/>
    /// - Minimum log level: Trace
    /// </summary>
    [FeatureId(nameof(Logging), nameof(F001))]
    public class F001 : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module(Logging.DI.Default.Configuration.C003, [],[])
        ];
    }
}
