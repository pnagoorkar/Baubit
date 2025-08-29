using Baubit.Collections;
using Baubit.Tasks;
using FluentResults;

namespace Baubit.Caching
{
    public class WaitingRoom<TValue> : IDisposable
    {
        public bool HasGuests { get => taskCompletionSources.Count > 0; }

        //private TaskCompletionSource<TValue> tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);

        private IList<TaskCompletionSource<TValue>> taskCompletionSources = new ConcurrentList<TaskCompletionSource<TValue>>();
        private bool disposedValue;

        public Task<TValue> Join(CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            taskCompletionSource.RegisterCancellationToken(cancellationToken);
            taskCompletionSources.Add(taskCompletionSource);
            cancellationToken.Register(HandleCancellation, taskCompletionSource);
            return taskCompletionSource.Task;
        }

        public Result TrySetResult(TValue value)
        {
            return taskCompletionSources.ToArray()
                                        .AsParallel()
                                        .Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => Result.OkIf(next.TrySetResult(value), "<TBD>")))
                                        .Bind(() => Result.Try(() => { taskCompletionSources.Clear(); }));
        }

        public Result TrySetCanceled()
        {
            return taskCompletionSources.ToArray()
                                        .AsParallel()
                                        .Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => Result.OkIf(next.TrySetCanceled(), "<TBD>")))
                                        .Bind(() => Result.Try(() => { taskCompletionSources.Clear(); }));
        }

        private void HandleCancellation(object? state, CancellationToken cancellationToken)
        {
            var taskCompletionSource = (TaskCompletionSource<TValue>)state;

            if (!taskCompletionSources.Remove(taskCompletionSource))
            {
                // log critical
            }
            taskCompletionSource.TrySetCanceled(cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TrySetCanceled();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
