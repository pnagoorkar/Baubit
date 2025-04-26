using Baubit.Validation;

namespace Baubit.DI
{
    public abstract class AModuleValidator<TModule> : AValidator<TModule> where TModule : IModule
    {
        protected AModuleValidator(string readableName) : base(readableName)
        {
        }
    }

}
