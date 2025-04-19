using Baubit.Testing;
using Baubit.Traceability;
using Baubit.Validation.Reasons;
using FluentResults;

namespace Baubit.Validation
{
    public interface IValidatable
    {
    }

    public static class ValidatableExtensions
    {
        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, string validatorKey, bool enforce = false) where TValidatable : IValidatable
        {
            var getResult = validatable.GetValidator(validatorKey);
            if (getResult.IsFailed)
            {
                if (getResult.Reasons.Any(reason => reason is ValidatorKeyNotSet) && !enforce)
                {
                    return Result.Ok(validatable).WithReasons(getResult.Reasons.GetNonErrors());
                }
                else
                {
                    return getResult.Map<TValidatable>(validator => default);
                }
            }
            else
            {
                return getResult.Value.Validate(validatable).AddSuccessIfPassed((r, s) => r.WithSuccesses(s), new PassedValidation<TValidatable>(validatorKey));
            }
        }
        public static Result<IValidator<TValidatable>> GetValidator<TValidatable>(this TValidatable validatable, string validatorKey) where TValidatable : IValidatable
        {
            AValidator<TValidatable> validator = null;
            return Result.FailIf(validatorKey == null, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ValidatorKeyNotSet())
                         .Bind(() => new ConfigurationSource<IValidator<TValidatable>>().Load(validatorKey))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ValidatorNotFound(validatorKey));
        }
    }
}
