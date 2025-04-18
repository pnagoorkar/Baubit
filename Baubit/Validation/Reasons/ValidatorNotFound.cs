using Baubit.Traceability.Reasons;

namespace Baubit.Validation.Reasons
{
    public class ValidatorNotFound : AReason
    {
        public ValidatorNotFound(string validatorKey) : base($"Validator with key {validatorKey} not found", default)
        {
        }
    }
}
