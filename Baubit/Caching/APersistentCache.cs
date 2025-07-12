using FluentResults;
using FluentResults.Extensions;
using System.Collections.Specialized;

namespace Baubit.Caching
{
    public abstract class APersistentCache<TValue> : IPersistentCache<TValue>
    {
        protected readonly ReaderWriterLockSlim Locker = new();

        protected abstract Result<IEntry<TValue>> Insert(TValue value);
        protected abstract Result<IEntry<TValue>> Fetch(long id);
        protected abstract Result<IEntry<TValue>> DeleteStorage(long id);
        protected abstract Result DeleteAll();
        protected abstract Result<long> GetCurrentCount();
        protected abstract void DisposeInternal();

        public Result<long> Add(TValue value)
        {
            Locker.EnterWriteLock();
            try { return Insert(value).Bind(entry => Result.Try(() => entry.Id)); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result<TValue> Get(long id)
        {
            Locker.EnterReadLock();
            try { return Fetch(id).Bind(entry => Result.Try(() => entry.Value)); }
            finally { Locker.ExitReadLock(); }
        }

        public Result<TValue> Remove(long id)
        {
            Locker.EnterWriteLock();
            try { return DeleteStorage(id).Bind(entry => Result.Try(() => entry.Value)); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result Clear()
        {
            Locker.EnterWriteLock();
            try{ return DeleteAll(); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result<long> Count()
        {
            Locker.EnterReadLock();
            try { return GetCurrentCount(); }
            finally { Locker.ExitReadLock(); }
        }

        public void Dispose()
        {
            Locker.Dispose();
            DisposeInternal();
        }
    }
}
