using FluentResults;

namespace Baubit.DI
{
    public interface IConstraint
    {
        public string ReadableName { get; }
        Result Check(List<IModule> modules);
    }

    public abstract class AConstraint : IConstraint
    {
        public string ReadableName { get; init; }
        protected AConstraint(string readableName)
        {
            ReadableName = readableName;
        }
        public abstract Result Check(List<IModule> modules);
    }

    public static class ConstraintExtensions
    {
        public static Result CheckAll(this IEnumerable<IConstraint> constraints, List<IModule> modules)
        {
            return constraints.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Check(modules)));
        }
    }
}
