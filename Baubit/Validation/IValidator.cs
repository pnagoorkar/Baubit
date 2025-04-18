using FluentResults;

namespace Baubit.Validation
{
    public interface IValidator<T>
    {
        public Result<T> Validate(T value);
    }
}
