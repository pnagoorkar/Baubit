namespace Baubit.Caching
{
    public abstract class ACacheAsyncEnumerator<TValue> : IAsyncEnumerator<IEntry<TValue>>, ICacheEnumerator
    {
        public IEntry<TValue>? Current { get; protected set; }
        public Guid? CurrentId => Current?.Id;

        protected readonly IOrderedCache<TValue> _cache;
        private Action<ICacheEnumerator> _onDispose;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration cancellationTokenRegistration;
        public ACacheAsyncEnumerator(IOrderedCache<TValue> cache,
                                    Action<ICacheEnumerator> onDispose,
                                    CancellationToken cancellationToken = default)
        {
            _cache = cache;
            _onDispose = onDispose;
            _cancellationToken = cancellationToken;
            cancellationTokenRegistration = _cancellationToken.Register(() => DisposeAsync());
        }

        public virtual ValueTask DisposeAsync()
        {
            _onDispose?.Invoke(this);
            cancellationTokenRegistration.Dispose();
            return ValueTask.CompletedTask;
        }

        public virtual async ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested) return false;
            try
            {
                Current = await _cache.GetNextAsync(CurrentId, _cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException tcExp)
            {
                // expected when _cancellationToken is cancelled
                return false;
            }
            return !_cancellationToken.IsCancellationRequested;
        }
    }
}
