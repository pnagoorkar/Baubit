using Baubit.DI;
using Baubit.Logging.DI.Default;

namespace Baubit.Logging.Features
{
    [FeatureId(nameof(Logging), nameof(F000))]
    public class F000 : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module(new Logging.DI.Default.Configuration{AddConsole = true }, [],[])
        ];
    }
}
