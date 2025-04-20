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
        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, Type concreteType) where TValidatable : IValidatable
        {
            return validatable.GetValidator(concreteType)
                              .Bind(validator => validator.Validate(validatable)
                                                          .AddSuccessIfPassed((r, s) => r.WithSuccesses(s), new PassedValidation<TValidatable>(validator.ReadableName)));
        }
        public static Result<IValidator<TValidatable>> GetValidator<TValidatable>(this TValidatable validatable, Type concreteType) where TValidatable : IValidatable
        {
            return Result.Try(() => new ConfigurationSource<IValidator<TValidatable>>())
                         .Bind(configSource => configSource.Load(concreteType));
        }
    }
}
