using Baubit.Validation;

namespace Baubit.Configuration
{
    public abstract class AConfigurationValidator<TConfiguration> : AValidator<TConfiguration> where TConfiguration : AConfiguration
    {
        protected AConfigurationValidator(string readableName) : base(readableName)
        {
        }
    }
}
