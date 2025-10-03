namespace Baubit.Caching
{
    public abstract class ACacheAsyncEnumerator<TValue> : IAsyncEnumerator<IEntry<TValue>>, ICacheEnumerator<IEntry<TValue>>
    {
        public IEntry<TValue>? Current { get; protected set; }
        public Guid? CurrentId => Current?.Id;

        protected readonly IOrderedCache<TValue> _cache;
        private Action<ICacheEnumerator<IEntry<TValue>>> _onDispose;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration cancellationTokenRegistration;
        public ACacheAsyncEnumerator(IOrderedCache<TValue> cache,
                                    Action<ICacheEnumerator<IEntry<TValue>>> onDispose,
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
            return true;
        }
    }
}
