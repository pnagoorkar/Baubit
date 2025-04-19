using FluentResults;
using System.Data;
using System.Linq.Expressions;

namespace Baubit.Validation
{
    public abstract class AValidator<T> : IValidator<T>
    {
        private List<Expression<Func<T, Result>>> _rules;
        protected AValidator()
        {
            _rules = GetRules().ToList();
        }

        protected abstract IEnumerable<Expression<Func<T, Result>>> GetRules();

        public Result<T> Validate(T value) => Result.Merge(_rules.Select(rule => rule.Compile()(value)).ToArray()).Bind(() => Result.Ok(value));

        public virtual void Dispose()
        {

        }
    }
}