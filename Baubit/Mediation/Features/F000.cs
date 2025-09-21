using Baubit.DI;
using Baubit.Mediation.DI;

namespace Baubit.Mediation.Features
{
    public class F000 : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Module(Baubit.Mediation.DI.Configuration.C000, [],[])
        ];
    }
}
