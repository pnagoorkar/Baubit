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
            ..new Baubit.Caching.InMemory.Features.F000<MyStatefulType.States>().Modules,
            ..new Baubit.Caching.InMemory.Features.F000<StateChanged<MyStatefulType.States>>().Modules,
            new Baubit.Logging.DI.Default.Module(new Baubit.Logging.DI.Default.Configuration { AddConsole = true, AddDebug = true }, [], [])
        ];
    }
}
