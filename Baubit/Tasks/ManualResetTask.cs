using FluentResults;
using FluentResults.Extensions;
using System.Threading;

namespace Baubit.Tasks
{
    public sealed class ManualResetTask<T>
    {
        private TaskCompletionSource<Result<T>> _tcs = null;
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public Task<Result<T>> WaitAsync()
        {
            _lock.EnterReadLock();
            try
            {
                return Result.FailIf(_tcs == null, "Cannot wait before resetting").Bind(() => _tcs.Task);
            }
            finally { _lock.ExitReadLock(); }
        }


        public Result Set(T value)
        {
            _lock.EnterReadLock();
            try
            {
                return _tcs == null ? Result.Ok() : _tcs.Task.IsCompleted ? Result.Ok() : Result.OkIf(_tcs.TrySetResult(value), "Failed to signal awaiter");
            }
            finally { _lock.ExitReadLock(); }
        }

        public Result Reset(CancellationToken cancellationToken = default)
        {
            _lock.EnterWriteLock();
            try
            {
                return _tcs == null ?
                       CreateNewTCS(cancellationToken).Bind(tcs => Result.Try(() => { _tcs = tcs; })) :
                       _tcs.Task.IsCompleted ?
                       CreateNewTCS(cancellationToken).Bind(tcs => Result.Try(() => { _tcs = tcs; })) :
                       Result.Ok();
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Result SetError(Exception exception)
        {
            _lock.EnterReadLock();
            try
            {
                return _tcs == null ? Result.Ok() : _tcs.Task.IsCompleted ? Result.Ok() : Result.OkIf(_tcs.TrySetException(exception), "Failed to relay exception");
            }
            finally { _lock.ExitReadLock(); }
        }

        private Result<TaskCompletionSource<Result<T>>> CreateNewTCS(CancellationToken cancellationToken = default)
        {
            return Result.Try(() => new TaskCompletionSource<Result<T>>(TaskCreationOptions.RunContinuationsAsynchronously))
                         .Bind(tcs => Result.Try(() => cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
                                            .Bind(canTokRegistration => Result.Try(() => { tcs.Task.ContinueWith(_ => canTokRegistration.Dispose(), TaskScheduler.Default); return tcs; })));
        }
    }
}
