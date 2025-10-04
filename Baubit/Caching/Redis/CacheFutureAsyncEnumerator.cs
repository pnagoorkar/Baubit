
using StackExchange.Redis;

namespace Baubit.Caching.Redis
{
    public class CacheFutureAsyncEnumerator<TValue> : Baubit.Caching.CacheFutureAsyncEnumerator<TValue>
    {
        private IDatabase _database;
        private string EnumerationHeadKey = "";
        public CacheFutureAsyncEnumerator(IOrderedCache<TValue> cache,
                                          Action<ICacheEnumerator> onDispose,
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
