using FluentResults;

namespace Baubit.Traceability.Errors
{
    public abstract class AError : IError
    {
        public List<IError> Reasons { get; init; } = new List<IError>();

        public virtual string Message { get; init; }

        public Dictionary<string, object> Metadata { get; init; }

        protected AError(List<IError> reasons, 
                         string message, 
                         Dictionary<string, object> metadata)
        {
            Reasons = reasons ?? new List<IError>();
            Message = message;
            Metadata = metadata;
        }
        protected AError() : this([], string.Empty, default)
        {

        }
    }
}
