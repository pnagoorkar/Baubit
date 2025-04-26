using Baubit.Validation;
using FluentResults;
using System.Linq.Expressions;

namespace Baubit.DI
{
    public class RootValidator<TRootModule> : AValidator<TRootModule> where TRootModule : IRootModule
    {
        public RootValidator() : base("Root validator")
        {
        }

        protected override IEnumerable<IConstraint<TRootModule>> GetConstraints() => Enumerable.Empty<IConstraint<TRootModule>>();

        protected override IEnumerable<Expression<Func<TRootModule, Result>>> GetRules()
        {
            return [root => root.CheckConstraints()];
        }
    }
}
