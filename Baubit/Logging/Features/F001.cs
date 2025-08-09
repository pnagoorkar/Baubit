using Baubit.DI;
using Baubit.Logging.DI.Default;

namespace Baubit.Logging.Features
{
    [FeatureId(nameof(Logging), nameof(F001))]
    public class F001 : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module(new Logging.DI.Default.Configuration{AddConsole = true, AddDebug = true, ConsoleLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace, DebugLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace }, [],[])
        ];
    }
}
