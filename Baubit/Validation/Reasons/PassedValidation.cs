using Baubit.Traceability.Successes;

namespace Baubit.Validation.Reasons
{
    public class PassedValidation<TValidatable> : ASuccess where TValidatable : IValidatable
    {
        public string ValidationKey { get; set; }
        public PassedValidation(string validationKey) : base("Passed validation!", default)
        {
            ValidationKey = validationKey;
        }
    }
}
