using Baubit.Validation;

namespace Baubit.DI
{
    public abstract class AModuleConfigurationValidator<TConfiguration> : AValidator<TConfiguration> where TConfiguration : AModuleConfiguration
    {

    }
}
