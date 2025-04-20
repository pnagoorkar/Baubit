using FluentResults;
using System.Data;
using System.Linq.Expressions;

namespace Baubit.Validation
{
    public abstract class AValidator<T> : IValidator<T>
    {
        public string ReadableName { get; init; }
        private List<Expression<Func<T, Result>>> _rules;
        protected AValidator(string readableName)
        {
            _rules = GetRules().ToList();
            ReadableName = readableName;
        }

        protected abstract IEnumerable<Expression<Func<T, Result>>> GetRules();

        public Result<T> Validate(T value) => Result.Merge(_rules.Select(rule => rule.Compile()(value)).ToArray()).Bind(() => Result.Ok(value));

        public virtual void Dispose()
        {

        }
    }
}