using Baubit.Reflection;
using FluentResults;

namespace Baubit.Validation
{
    public interface IValidator<T> : ISelfContained
    {
        public Result<T> Validate(T value);
    }
}
