

namespace Baubit.Caching
{
    public class CacheAsyncEnumerator<TValue> : ACacheAsyncEnumerator<TValue>
    {
        public CacheAsyncEnumerator(IOrderedCache<TValue> cache, 
                                    Action<ICacheEnumerator<IEntry<TValue>>> onDispose, 
                                    CancellationToken cancellationToken = default) : base(cache, onDispose, cancellationToken)
        {
        }
    }
}
