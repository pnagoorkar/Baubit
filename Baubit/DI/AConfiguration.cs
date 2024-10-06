namespace Baubit.DI
{
    public abstract class AConfiguration : Configuration.AConfiguration
    {
        public string ModuleValidatorKey { get; init; } = "default";
    }
}
