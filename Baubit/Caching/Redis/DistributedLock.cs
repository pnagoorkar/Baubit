using StackExchange.Redis;
using static Pipelines.Sockets.Unofficial.Threading.MutexSlim;

namespace Baubit.Caching.Redis
{
    public sealed class DistributedLock : IDisposable
    {
        private IDatabase _database;
        private string _lockKey;
        private string _lockToken = Guid.NewGuid().ToString("N");
        private bool disposedValue;

        private DistributedLock(IDatabase database, 
                                string lockKey)
        {
            _database = database;
            _lockKey = lockKey;
        }

        public static DistributedLock Take(IDatabase database, 
                                           string lockKey, 
                                           TimeSpan ttl)
        {
            var distributedLock = new DistributedLock(database, lockKey);
            int delayMs = 5;
            while (!database.LockTake(distributedLock._lockKey, distributedLock._lockToken, ttl))
            {
                Thread.Sleep(delayMs);
                if (delayMs < 100) delayMs *= 2; // small backoff cap
            }
            return distributedLock;
        }

        public bool Release()
        {
            return _database.LockRelease(_lockKey, _lockToken);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Release();
                    _database = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
