namespace Baubit.Caching
{
    public class CacheFutureAsyncEnumerator<TValue> : ACacheAsyncEnumerator<TValue>
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
