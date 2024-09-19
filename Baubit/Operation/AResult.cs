
//namespace Baubit.Operation
//{
//    public abstract class AResult : IResult
//    {
//        public bool? Success { get; }
//        public object? Value { get; }
//        public string? FailureMessage { get; }
//        public object? FailureSupplement { get; }
//        public Exception? Exception { get; }
//        public AResult(bool? success, object? value)
//        {
//            Success = success;
//            Value = value;
//        }
//        public AResult(bool? success, string? failureMessage, object? failureSupplement)
//        {
//            Success = success;
//            FailureMessage = failureMessage;
//            FailureSupplement = failureSupplement;
//        }
//        public AResult(Exception? exception)
//        {
//            Exception = exception;
//        }
//    }
//    public abstract class AResult<TValue> : AResult
//    {
//        public new TValue? Value { get => (TValue)base.Value!; }
//        protected AResult(Exception? exception) : base(exception)
//        {
//        }

//        protected AResult(bool? success, TValue? value) : base(success, value)
//        {
//        }

//        protected AResult(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
//        {
//        }
//    }
//}
