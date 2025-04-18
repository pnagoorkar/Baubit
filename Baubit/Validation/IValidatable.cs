using Baubit.Traceability.Errors;
using Baubit.Validation.Reasons;
using FluentResults;

namespace Baubit.Validation
{
    public interface IValidatable
    {
    }

    public static class ValidatableExtensions
    {
        //public static Result<IValidator<TValidatable>> TryGetValidator<TValidatable>(this TValidatable validatable, string validatorKey) where TValidatable : IValidatable
        //{
        //    validatable.TryGetValidator<TValidatable>(validatorKey);
        //}
        public static Result<IValidator<TValidatable>> GetValidator<TValidatable>(this TValidatable validatable, string validatorKey) where TValidatable : IValidatable
        {
            AValidator<TValidatable> validator = null;
            return Result.OkIf(AValidator<TValidatable>.CurrentValidators.TryGetValue(validatorKey, out validator), 
                               new CompositeError<TValidatable>([new ValidatorNotFound(validatorKey)], null, "", null))
                         .Bind(() => Result.Ok<IValidator<TValidatable>>(validator));
        }
    }
}
