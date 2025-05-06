using Baubit.Traceability.Reasons;

namespace Baubit.DI.Constraints.Reasons
{
    public class DependencyCheckFailed : AReason
    {
        public Type MissingDependencyType { get; init; }
        public DependencyCheckFailed(Type missingDependencyType)
        {
            MissingDependencyType = missingDependencyType;
        }
    }
}
