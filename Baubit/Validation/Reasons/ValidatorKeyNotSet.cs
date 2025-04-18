using Baubit.Traceability.Reasons;

namespace Baubit.Validation.Reasons
{
    public class ValidatorKeyNotSet : AReason
    {
        public ValidatorKeyNotSet()
        {
            Message = $"Validator key not set";
        }
    }
}
