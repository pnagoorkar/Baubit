using FluentResults;

namespace Baubit.Traceability.Reasons
{
    public abstract class AReason : IReason
    {
        public virtual string Message => "";

        public Dictionary<string, object> Metadata => new Dictionary<string, object>();
    }
}
