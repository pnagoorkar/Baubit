using Baubit.Tasks;
using FluentResults;
using FluentResults.Extensions;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    public abstract class AOrderedCache<TValue> : IOrderedCache<TValue>
    {
        protected readonly ReaderWriterLockSlim Locker = new();

        protected abstract Result<IEntry<TValue>> Insert(TValue value);
        protected abstract Result<IEntry<TValue>> Fetch(long id);
        protected abstract Result<IEntry<TValue>> DeleteStorage(long id);
        protected abstract Result DeleteAll();
        protected abstract Result<long> GetCurrentCount();
        protected abstract void DisposeInternal();

        protected abstract Result Upsert(IEnumerable<Metadata> metadata);

        protected abstract Result<Metadata> GetCurrentHead();
        protected abstract Result<Metadata> GetCurrentTail();
        protected abstract Result<Metadata> GetMetadata(long id);
        protected abstract Result DeleteMetadata(long id);
        protected abstract Result DeleteAllMetadata();

        public Result<IEntry<TValue>> Add(TValue value)
        {
            Locker.EnterWriteLock();
            try { return Insert(value).Bind(entry => nextSignal.Set(entry.Id).Bind(() => Result.Ok(entry))); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result<IEntry<TValue>> Get(long id)
        {
            Locker.EnterReadLock();
            try { return Fetch(id); }
            finally { Locker.ExitReadLock(); }
        }

        public Result<IEntry<TValue>> Remove(long id)
        {
            Locker.EnterWriteLock();
            try { return DeleteStorage(id).Bind(entry => RemoveMetadata(entry.Id).Bind(() => Result.Ok(entry))); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result Clear()
        {
            Locker.EnterWriteLock();
            try{ return DeleteAll().Bind(() => DeleteAllMetadata()); }
            finally { Locker.ExitWriteLock(); }
        }

        public Result<long> Count()
        {
            Locker.EnterReadLock();
            try { return GetCurrentCount(); }
            finally { Locker.ExitReadLock(); }
        }

        ManualResetTask<long> nextSignal = new ManualResetTask<long>();
        public Task<Result<IEntry<TValue>>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default)
        {
            Locker.EnterReadLock();
            try
            {
                if (id == null)
                {
                    var getHeadResult = GetCurrentHead();
                    if(getHeadResult.ValueOrDefault == null) //there is no data in the cache
                    {
                        return AwaitNextAsync(cancellationToken);
                    }
                    else
                    {
                        return Task.FromResult(getHeadResult.Bind(metadata => Fetch(metadata.Id)));
                    }
                }
                else
                {
                    return GetMetadata(id.Value).Bind(metadata => metadata.Next == null ? AwaitNextAsync(cancellationToken) : Task.FromResult(Fetch(metadata.Next.Value)));
                }
            }
            finally { Locker.ExitReadLock(); }
        }

        private Task<Result<IEntry<TValue>>> AwaitNextAsync(CancellationToken cancellationToken = default)
        {
            return nextSignal.Reset(cancellationToken).Bind(() => nextSignal.WaitAsync().Bind(nextId => Fetch(nextId)));
        }

        private Result<IEntry<TValue>> AddTail(IEntry<TValue> entry)
        {
            return Result.Try(() => new Metadata { Id = entry.Id })
                         .Bind(newTail => GetCurrentTail().Bind(currentTail => AddTail(currentTail, newTail).Bind(() => Upsert(currentTail == null ? [newTail] : [currentTail, newTail]))))
                         .Bind(() => Result.Ok(entry));
        }

        private Result AddTail(Metadata currentTail, Metadata newTail)
        {
            return currentTail == null ? 
                   Result.Ok() : 
                   Result.Try(() => currentTail.Next = newTail.Id)
                         .Bind(_ => Result.Try(() => newTail.Previous = currentTail.Id))
                         .Bind(_ => Result.Ok());
        }

        private Result RemoveMetadata(long id)
        {
            Metadata current = null;
            List<Metadata> upsertables = new List<Metadata>();

            return Result.Try(() =>
                   {
                       current = GetMetadata(id).ValueOrDefault;
                       if (current == null) return;
                   
                       var previous = current.Previous == null ? null : GetMetadata(current.Previous.Value).ValueOrDefault;
                       var next = current.Next == null ? null : GetMetadata(current.Next.Value).ValueOrDefault;
                   
                       if (previous != null)
                       {
                           previous.Next = next?.Id;
                           upsertables.Add(previous);
                       }
                   
                       if (next != null)
                       {
                           next.Previous = previous?.Id;
                           upsertables.Add(next);
                       }
                   
                   }).Bind(() => upsertables.Count == 0 ? Result.Ok() : Upsert(upsertables)).Bind(() => current == null ? Result.Ok() : DeleteMetadata(current.Id));
        }

        public void Dispose()
        {
            Locker.Dispose();
            DisposeInternal();
        }
    }

    public static class CachingExtensions
    {
        public static async IAsyncEnumerable<Result<IEntry<TValue>>> ReadAsync<TCache, TValue>(this TCache cache, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TCache : IOrderedCache<TValue>
        {
            long? id = null;
            do
            {
                yield return await cache.GetNextAsync(id, cancellationToken).Bind(entry => Result.Try(() => { id = entry.Id; return entry; }));

            } while (!cancellationToken.IsCancellationRequested);
        }
    }
}
