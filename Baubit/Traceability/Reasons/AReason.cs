using FluentResults;

namespace Baubit.Traceability.Reasons
{
    public abstract class AReason : IReason
    {
        public string Message { get; init; }

        public Dictionary<string, object> Metadata { get; init; }
        protected AReason(string message, Dictionary<string, object> metadata)
        {
            Message = message;
            Metadata = metadata;
        }
    }
}
