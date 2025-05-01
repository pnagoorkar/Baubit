using FluentResults;

namespace Baubit.Validation
{
    public interface IValidator
    {
        Result Validate();
    }
    public interface IValidator<T> where T : IValidatable
    {
        public string ReadableName { get; }
        public Result<T> Validate(T value);
    }
}
