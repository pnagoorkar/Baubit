using StackExchange.Redis;

namespace Baubit.Caching.Redis
{
    public class CacheAsyncEnumerator<TValue> : Baubit.Caching.CacheAsyncEnumerator<TValue>
    {
        private IDatabase _database;
        private string EnumerationHeadKey = "";
        public CacheAsyncEnumerator(IOrderedCache<TValue> cache, 
                                    Action<ICacheEnumerator<IEntry<TValue>>> onDispose, 
                                    IDatabase database,
                                    CancellationToken cancellationToken = default) : base(cache, onDispose, cancellationToken)
        {
            _database = database;
        }

        public override ValueTask DisposeAsync()
        {
            _database = null;
            return base.DisposeAsync();
        }

        public override ValueTask<bool> MoveNextAsync()
        {
            // TODO - Update Global Enumeration Head
            return base.MoveNextAsync();
        }
    }
}
