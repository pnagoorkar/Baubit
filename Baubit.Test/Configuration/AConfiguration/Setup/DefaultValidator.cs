using Baubit.Validation;
using FluentResults;
using System.Linq.Expressions;

namespace Baubit.Test.Configuration.AConfiguration.Setup
{
    public class DefaultValidator : AValidator<Configuration>
    {
        public DefaultValidator() : base("Default validator")
        {
            
        }

        protected override IEnumerable<Expression<Func<Configuration, Result>>> GetRules()
        {
            return [v => Result.Ok()];
        }
    }
}
