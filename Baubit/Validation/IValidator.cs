using Baubit.Reflection;
using FluentResults;

namespace Baubit.Validation
{
    public interface IValidator<T> : ISelfContained
    {
        public string ReadableName { get; }
        public Result<T> Validate(T value);
    }
}
