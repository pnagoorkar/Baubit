using Baubit.DI;

namespace Baubit.Test.DI.Setup
{
    public record ModuleConfiguration : AConfiguration
    {
        public string SomeString { get; init; }
        public string SomeSecretString { get; init; }
    }
}
