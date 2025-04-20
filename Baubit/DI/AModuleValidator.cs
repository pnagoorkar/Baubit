using Baubit.Traceability;
using Baubit.Traceability.Reasons;
using Baubit.Validation;
using FluentResults;
using System.Linq.Expressions;

namespace Baubit.DI
{
    public abstract class AModuleValidator<TModule> : AValidator<TModule> where TModule : IModule
    {
        protected AModuleValidator(string readableName) : base(readableName)
        {
        }

        public abstract IEnumerable<Expression<Func<List<IModule>, Result>>> GetConstraints();
    }

    public class SingularityConstraint<TModule> : AModuleValidator<TModule> where TModule : IModule
    {
        public SingularityConstraint() : base("Singularity constraint")
        {
        }

        public override IEnumerable<Expression<Func<List<IModule>, Result>>> GetConstraints()
        {
            return [modules => Result.OkIf(modules.Count(m => m is TModule) == 1, new Error(string.Empty)).AddReasonIfFailed((res, reas) => res.WithReasons(reas), new SingularityCheckFailed())];
        }

        protected override IEnumerable<Expression<Func<TModule, Result>>> GetRules() => Enumerable.Empty<Expression<Func<TModule, Result>>>();
    }

    public class SingularityCheckFailed : AReason
    {

    }
}
