using Baubit.Validation;
using FluentResults;
using System.Linq.Expressions;

namespace Baubit.Test.Validation.Setup
{
    public class Validator : AValidator<Validatable>
    {
        protected override IEnumerable<Expression<Func<Validatable, Result>>> GetRules()
        {
            return [v => Result.Ok()];
        }
    }
}
