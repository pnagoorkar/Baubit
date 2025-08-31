using Baubit.Collections;
using Baubit.Tasks;
using FluentResults;
using System.Threading.Tasks;

namespace Baubit.Caching
{
    public class WaitingRoom<TValue> : IDisposable
    {
        public bool HasGuests { get => _numOfGuests > 0; }

        private TaskCompletionSource<TValue> tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);

        private volatile int _numOfGuests = 0;

        private bool disposedValue;

        public async Task<TValue> Join(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _numOfGuests);
            return await tcs.Task.WaitAsync(cancellationToken);
        }

        public Result TrySetResult(TValue value)
        {
            return Result.Try(() => tcs.TrySetResult(value)).Bind(success => Result.OkIf(success, "<TBD>"));
        }

        public Result TrySetCanceled(CancellationToken cancellationToken = default)
        {
            return Result.Try(() => tcs.TrySetCanceled(cancellationToken)).Bind(success => Result.OkIf(success, "<TBD>"));
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
