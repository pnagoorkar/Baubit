using FluentResults;

namespace Baubit.Tasks
{
    public static class TaskExtensions
    {
        public static Result Wait(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                task.Wait();
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }
        public static async Task<Result> WaitAsync(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                await task.ConfigureAwait(false);
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }

        public static Result RegisterCancellationToken<T>(this TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken)
        {
            return Result.Try(() => cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
                         .Bind(registration => Result.Try(() => { taskCompletionSource.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default); }));
        }
    }
}
