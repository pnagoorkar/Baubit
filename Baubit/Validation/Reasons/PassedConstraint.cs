using Baubit.Traceability.Successes;

namespace Baubit.Validation.Reasons
{
    public class PassedConstraint : ASuccess
    {
        public PassedConstraint(string constraintName) : base($"Passed constraint: {constraintName}", default)
        {
            
        }
    }
}
