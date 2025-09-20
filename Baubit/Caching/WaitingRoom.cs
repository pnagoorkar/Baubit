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
            return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public bool TrySetResult(TValue value)
        {
            return tcs.TrySetResult(value);
        }

        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            return tcs.TrySetCanceled(cancellationToken);
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
