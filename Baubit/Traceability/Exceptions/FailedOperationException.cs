using FluentResults;

namespace Baubit.Traceability.Exceptions
{
    public class FailedOperationException<TResult> : Exception where TResult : IResultBase
    {
        public TResult Result { get; init; }
        public FailedOperationException(TResult result) : base(result.ToString())
        {
            this.Result = result;
        }
    }
}
