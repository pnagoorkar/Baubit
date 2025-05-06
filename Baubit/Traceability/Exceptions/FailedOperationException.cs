using FluentResults;

namespace Baubit.Traceability.Exceptions
{
    public class FailedOperationException: Exception 
    {
        public IResultBase Result { get; init; }

        public FailedOperationException(IResultBase result) : base(result.ToString())
        {
            this.Result = result;
        }
    }
}
