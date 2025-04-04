using FluentResults;

namespace Baubit.Traceability.Errors
{
    public abstract class AError : IError
    {
        public List<IReason> NonErrorReasons { get; init; } = new List<IReason>();

        public List<IError> Reasons { get; init; } = new List<IError>();

        public string Message { get; init; }

        public Dictionary<string, object> Metadata { get; init; }

        protected AError(List<IReason> nonErrorReasons, 
                         List<IError> reasons, 
                         string message, 
                         Dictionary<string, object> metadata)
        {
            NonErrorReasons = nonErrorReasons;
            Reasons = reasons;
            Message = message;
            Metadata = metadata;
        }
    }
}
