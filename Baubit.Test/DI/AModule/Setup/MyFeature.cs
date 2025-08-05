using Baubit.Configuration;
using Baubit.DI;

namespace Baubit.Test.DI.AModule.Setup
{
    [FeatureId("feature1")]
    public class MyFeature : IFeature
    {
        public IEnumerable<IModule> Modules => [new Module(ConfigurationSource.Empty)];
    }
}
