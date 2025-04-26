using Baubit.Traceability.Reasons;
using FluentResults;

namespace Baubit.Validation
{
    public interface IConstrainable
    {
    }
    public interface IConstraint<TConstrainable> where TConstrainable : IConstrainable
    {
        public string ReadableName { get; }
        public Result<TConstrainable> Check(TConstrainable constrainable, IEnumerable<IConstrainable> constrainables);
    }

    public abstract class AConstraint<TConstrainable> : IConstraint<TConstrainable> where TConstrainable : IConstrainable
    {
        public string ReadableName { get; init; }
        protected AConstraint(string readableName)
        {
            ReadableName = readableName;
        }
        public abstract Result<TConstrainable> Check(TConstrainable constrainable, IEnumerable<IConstrainable> constrainables);
    }

    public class SingularityConstraint<TConstrainable> : AConstraint<TConstrainable> where TConstrainable : IConstrainable
    {
        public SingularityConstraint() : base("Singularity constraint")
        {
        }

        public override Result<TConstrainable> Check(TConstrainable constrainable, IEnumerable<IConstrainable> constrainables)
        {
            return constrainables.Count(constrainable => constrainable is TConstrainable) == 1 ? 
                   Result.Ok(constrainable) : 
                   Result.Fail(string.Empty).WithReason(new SingularityCheckFailed());
        }
    }
    public class SingularityCheckFailed : AReason
    {

    }
}
