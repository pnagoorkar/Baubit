using System.Collections;
using System.Threading;

namespace Baubit.Caching
{
    public class CacheEnumerator<TEntry, TValue> : IEnumerator<TEntry> where TEntry : IEntry<TValue>
    {
        public TEntry Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public interface ICacheEnumerator<TValue>
    {
        public long? CurrentId { get; }
    }

    public class CacheAsyncEnumerator<TValue> : IAsyncEnumerator<IEntry<TValue>>, ICacheEnumerator<IEntry<TValue>>
    {
        public IEntry<TValue>? Current { get; protected set; }
        public long? CurrentId => Current?.Id;

        protected readonly IOrderedCache<TValue> _cache;
        private Action<ICacheEnumerator<IEntry<TValue>>> _onDispose;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration cancellationTokenRegistration;
        public CacheAsyncEnumerator(IOrderedCache<TValue> cache,
                                    Action<ICacheEnumerator<IEntry<TValue>>> onDispose, 
                                    CancellationToken cancellationToken = default)
        {
            _cache = cache;
            _onDispose = onDispose;
            _cancellationToken = cancellationToken;
            cancellationTokenRegistration = _cancellationToken.Register(() => DisposeAsync());
        }

        public ValueTask DisposeAsync()
        {
            _onDispose?.Invoke(this);
            cancellationTokenRegistration.Dispose();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested) return false;
            Current = await _cache.GetNextAsync(CurrentId).ConfigureAwait(false);
            return true;
        }
    }

    public interface IFutureAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        //new IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
        IAsyncEnumerator<T> GetFutureAsyncEnumerator(CancellationToken cancellationToken = default);
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetFutureAsyncEnumerator(cancellationToken);
    }

    public class CacheFutureAsyncEnumerator<TValue> : CacheAsyncEnumerator<TValue>
    {
        public CacheFutureAsyncEnumerator(IOrderedCache<TValue> cache, 
                                          Action<ICacheEnumerator<IEntry<TValue>>> onDispose,
                                          CancellationToken cancellationToken = default) : base(cache, onDispose, cancellationToken)
        {
            cache.GetLastOrDefault(out var lastEntry);
            Current = lastEntry; // this to ensure the evictor knows we are not interested in any entries through the current tail
        }
    }
}
