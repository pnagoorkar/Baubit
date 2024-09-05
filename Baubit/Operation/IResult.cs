namespace Baubit.Operation
{
    public interface IResult
    {
        public bool? Success { get; }
        public object? Value { get; }
        public string? FailureMessage { get; }
        public object? FailureSupplement { get; }
        public Exception? Exception { get; }
    }
}
