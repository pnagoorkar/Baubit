using FluentResults;
using FluentResults.Extensions;
using System.Collections.Specialized;

namespace Baubit.Caching
{
    public abstract class APersistentCache<TValue> : IPersistentCache<TValue>
    {
        public long Count { get; private set; }
        protected readonly ReaderWriterLockSlim Locker = new();

        protected abstract Task<Result<IEntry<TValue>>> InsertAsync(TValue value);
        protected abstract Task<Result<IEntry<TValue>>> FetchAsync(long id);
        protected abstract Task<Result<IEntry<TValue>>> DeleteStorageAsync(long id);
        protected abstract void DisposeInternal();

        public async Task<Result<long>> AddAsync(TValue value)
        {
            Locker.EnterWriteLock();
            try { return await InsertAsync(value).Bind(entry => Result.Try(() => { Count++; return entry.Id; })); }
            finally { Locker.ExitWriteLock(); }
        }

        public async Task<Result<TValue>> GetAsync(long id)
        {
            Locker.EnterReadLock();
            try { return await FetchAsync(id).Bind(entry => Result.Try(() => entry.Value)); }
            finally { Locker.ExitReadLock(); }
        }

        public async Task<Result<TValue>> RemoveAsync(long id)
        {
            Locker.EnterWriteLock();
            try { return await DeleteStorageAsync(id).Bind(entry => Result.Try(() => { Count--; return entry.Value; })); }
            finally { Locker.ExitWriteLock(); }
        }

        public void Dispose()
        {
            Locker.Dispose();
            DisposeInternal();
        }
    }
}
