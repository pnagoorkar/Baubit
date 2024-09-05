namespace Baubit.Operation
{
    public interface IOperation
    {

    }
    public interface IOperation<TContext, TResult> : IOperation where TContext : IContext where TResult : IResult
    {
        public Task<TResult> RunAsync(TContext context);
    }
}
