using Baubit.Configuration;
using Baubit.DI;
using Baubit.Traceability;
using Baubit.Validation.Reasons;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Validation
{
    public interface IValidatable : IConstrainable
    {
    }

    public static class ValidatableExtensions
    {

        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, List<Type> concreteType, List<IConstrainable> constrainables) where TValidatable : class, IValidatable
        {
            return concreteType.Aggregate(Result.Ok(validatable), (seed, next) => seed.Bind(validatable => validatable.TryValidate(next, constrainables)));
        }

        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, Type concreteType, List<IConstrainable> constrainables) where TValidatable : class, IValidatable
        {
            return validatable.LoadContext(concreteType).Bind(context => context.Execute(constrainables)).Bind(() => Result.Ok(validatable));
        }

        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, List<Type> concreteType) where TValidatable : class, IValidatable
        {
            return concreteType.Aggregate(Result.Ok(validatable), (seed, next) => seed.Bind(validatable => validatable.TryValidate(next)));
        }

        public static Result<TValidatable> TryValidate<TValidatable>(this TValidatable validatable, Type concreteType) where TValidatable : class, IValidatable
        {
            return validatable.LoadContext(concreteType).Bind(context => context.Execute()).Bind(() => Result.Ok(validatable));
        }

        public static Result<IValidationContext> LoadContext<TValidatable>(this TValidatable validatable, Type validatorConcreteType) where TValidatable : class, IValidatable
        {
            //Func<IServiceCollection, IServiceCollection> registrationHandler =
            //    services => services.AddSingleton(validatable.GetType(), validatable)
            //                        .AddSingleton(serviceType: typeof(IValidator<>).MakeGenericType(validatable.GetType()), implementationType: validatorConcreteType)
            //                        .AddSingleton(serviceType: typeof(IValidationContext), implementationType: typeof(ValidationContext<>).MakeGenericType(validatable.GetType()));
            //return ConfigurationBuilder.CreateNew()
            //                           .Bind(cB => cB.Build())
            //                           .Bind(config => config.TryGetRequiredService<IValidationContext>(services => services.AddSingleton(validatable.GetType(), validatable)
            //                        .AddSingleton(serviceType: typeof(IValidator<>).MakeGenericType(validatable.GetType()), implementationType: validatorConcreteType)
            //                        .AddSingleton(serviceType: typeof(IValidationContext), implementationType: typeof(ValidationContext<>).MakeGenericType(validatable.GetType()))));

            return Result.Try(() => new ServiceCollection())
                         .Bind(services => Result.Try(() => services.AddSingleton(validatable.GetType(), validatable)))
                         .Bind(services => Result.Try(() => services.AddSingleton(serviceType: typeof(IValidator<>).MakeGenericType(validatable.GetType()), implementationType: validatorConcreteType)))
                         .Bind(services => Result.Try(() => services.AddSingleton(serviceType: typeof(IValidationContext), implementationType: typeof(ValidationContext<>).MakeGenericType(validatable.GetType()))))
                         .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<IValidationContext>()));
        }
    }

    public interface IValidationContext
    {
        Result Execute();
        Result Execute(List<IConstrainable> constrainables);
    }

    public class ValidationContext<TValidatable> : IValidationContext where TValidatable: class, IValidatable
    {
        public IValidator<TValidatable> Validator { get; init; }
        public TValidatable Validatable { get; init; }
        public ValidationContext(IValidator<TValidatable> validator, TValidatable validatable)
        {
            Validator = validator;
            Validatable = validatable;
        }

        public Result Execute() => Validator.Validate(Validatable).AddSuccessIfPassed((res, successes) => res.WithSuccesses(successes), new PassedValidation<TValidatable>(string.Empty)).Bind(v => Result.Ok());

        public Result Execute(List<IConstrainable> constrainables)
        {
            return Validator.Constraints.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Check(Validatable, constrainables)).Bind(v => Result.Ok()));
        }
    }
}
