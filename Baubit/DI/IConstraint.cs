using Baubit.Traceability.Reasons;
using Baubit.Traceability;
using FluentResults;

namespace Baubit.DI
{
    public interface IConstraint
    {
        public string ReadableName { get; }
        Result Check(List<IModule> modules);
    }

    public static class ConstraintExtensions
    {
        public static Result CheckAll(this IEnumerable<IConstraint> constraints, List<IModule> modules)
        {
            return constraints.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Check(modules)));
        }
    }

    public class SingularityConstraint<TModule> : IConstraint
    {
        public string ReadableName => string.Empty;

        public Result Check(List<IModule> modules)
        {
            return modules.Count(mod => mod is TModule) == 1 ? Result.Ok() : Result.Fail(string.Empty).AddReasonIfFailed(new SingularityCheckFailed());
        }
    }
    public class SingularityCheckFailed : AReason
    {

    }
}
