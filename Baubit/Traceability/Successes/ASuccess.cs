using Baubit.Traceability.Reasons;
using FluentResults;

namespace Baubit.Traceability.Successes
{
    public abstract class ASuccess : AReason, ISuccess
    {
        protected ASuccess(string message, Dictionary<string, object> metadata) : base(message, metadata)
        {
        }
        protected ASuccess() : this(string.Empty, default)
        {
            
        }
    }
}
