using FluentResults;

namespace Baubit.Traceability.Reasons
{
    public abstract class AReason : IReason
    {
        public virtual string Message { get; init; }

        public Dictionary<string, object> Metadata { get; init; }
        protected AReason(string message, Dictionary<string, object> metadata)
        {
            Message = message;
            Metadata = metadata;
        }

        protected AReason() : this(string.Empty, default)
        {

        }

        public override string ToString()
        {
            return Message;
        }
    }
}
