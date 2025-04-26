using FluentResults;
using System.Data;
using System.Linq.Expressions;

namespace Baubit.Validation
{
    public abstract class AValidator<T> : IValidator<T> where T : IValidatable
    {
        public string ReadableName { get; init; }
        public List<IConstraint<T>> Constraints { get; init; } = new List<IConstraint<T>>();
        private List<Expression<Func<T, Result>>> _rules;
        protected AValidator(string readableName)
        {
            _rules = GetRules().ToList();
            Constraints = GetConstraints().ToList();
            ReadableName = readableName;
        }

        protected abstract IEnumerable<Expression<Func<T, Result>>> GetRules();
        protected virtual IEnumerable<IConstraint<T>> GetConstraints() => Enumerable.Empty<IConstraint<T>>();

        public Result<T> Validate(T value) => Result.Merge(_rules.Select(rule => rule.Compile()(value)).ToArray()).Bind(() => Result.Ok(value));

        public virtual void Dispose()
        {

        }
    }
}