using Baubit.DI;

namespace Baubit.Test.DI.Setup
{
    public class ModuleConfiguration : AConfiguration
    {
        public string SomeString { get; init; }
        public string SomeSecretString { get; init; }
    }
}
