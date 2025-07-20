using FluentResults;
using FluentResults.Extensions;

namespace Baubit.Tasks
{
    public sealed class ManualResetTask<T>
    {
        private TaskCompletionSource<Result<T>> _tcs = null;

        public Task<Result<T>> WaitAsync() => Result.FailIf(_tcs == null, "Cannot wait before resetting").Bind(() => _tcs.Task);

        public Result Set(T value)
        {
            return _tcs == null ? Result.Ok() : _tcs.Task.IsCompleted ? Result.Ok() : Result.OkIf(_tcs.TrySetResult(value), "Failed to signal awaiter");
        }

        public Result Reset(CancellationToken cancellationToken = default)
        {
            return _tcs == null ? 
                   CreateNewTCS(cancellationToken).Bind(tcs => Result.Try(() => { _tcs = tcs; })) : 
                   _tcs.Task.IsCompleted ?
                   CreateNewTCS(cancellationToken).Bind(tcs => Result.Try(() => { _tcs = tcs; })) : 
                   Result.Ok();
        }

        private Result<TaskCompletionSource<Result<T>>> CreateNewTCS(CancellationToken cancellationToken = default)
        {
            return Result.Try(() => new TaskCompletionSource<Result<T>>(TaskCreationOptions.RunContinuationsAsynchronously))
                         .Bind(tcs => Result.Try(() => cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
                                            .Bind(canTokRegistration => Result.Try(() => { tcs.Task.ContinueWith(_ => canTokRegistration.Dispose(), TaskScheduler.Default); return tcs; })));
        }
    }
}
