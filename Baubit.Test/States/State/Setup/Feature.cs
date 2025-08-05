using Baubit.Configuration;
using Baubit.DI;
using Baubit.States;

namespace Baubit.Test.States.State.Setup
{
    public class Feature : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Baubit.States.DI.Module<MyStatefulType.States>(ConfigurationSource.Empty),
            new Baubit.Caching.Default.DI.Module<MyStatefulType.States>(ConfigurationSource.Empty),
            new Baubit.Caching.Default.DI.Module<StateChanged<MyStatefulType.States>>(ConfigurationSource.Empty),
            new Baubit.Logging.DI.Default.Module(new Baubit.Logging.DI.Default.Configuration { AddConsole = true, AddDebug = true }, [], [])
        ];
    }
}
