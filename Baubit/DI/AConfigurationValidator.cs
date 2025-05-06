namespace Baubit.DI
{
    public abstract class AConfigurationValidator<TConfiguration> : Configuration.AConfigurationValidator<TConfiguration> where TConfiguration : Configuration.AConfiguration
    {
        protected AConfigurationValidator(string readableName) : base(readableName)
        {
        }
    }
}
