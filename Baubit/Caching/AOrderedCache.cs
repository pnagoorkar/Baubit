using Baubit.Collections;
using Baubit.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    /// <summary>
    /// Provides a base implementation for an ordered cache with thread-safe operations.
    /// Manages storage, retrieval, and metadata for cache entries.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public abstract class AOrderedCache<TValue> : IOrderedCache<TValue>
    {
        /// <summary>
        /// Synchronizes access to cache operations for thread safety.
        /// </summary>
        protected readonly ReaderWriterLockSlim Locker = new();

        private readonly ILogger<AOrderedCache<TValue>> _logger;

        private bool disposedValue;
        private int _l1StoreCurrentCap;
        private LinkedList<IEntry<TValue>> _l1Store = new LinkedList<IEntry<TValue>>();
        private Dictionary<long, LinkedListNode<IEntry<TValue>>> l1Lookup = new Dictionary<long, LinkedListNode<IEntry<TValue>>>();
        private volatile bool areReadersWaiting = false;
        private TaskCompletionSource<bool> nextGenAwaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Configuration Configuration { get; init; }
        public int L1StoreCurrentCap { get => _l1StoreCurrentCap; private set => _l1StoreCurrentCap = value; }
        public int L1StoreCount => _l1Store.Count;

        protected AOrderedCache(Configuration cacheConfiguration, 
                                ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AOrderedCache<TValue>>();
            Configuration = cacheConfiguration;
            L1StoreCurrentCap = Configuration.L1StoreCap;
        }

        /// <summary>
        /// Inserts a value into the cache storage.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>A result containing the created entry or an error.</returns>
        protected abstract Result<IEntry<TValue>> Insert(TValue value);

        /// <summary>
        /// Fetches an entry from the cache by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entry.</param>
        /// <returns>A result containing the entry or an error.</returns>
        protected abstract Result<IEntry<TValue>> Fetch(long id);

        protected abstract Result<IEntry<TValue>> FetchNext(long id);

        /// <summary>
        /// Deletes an entry from the cache storage by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entry to delete.</param>
        /// <returns>A result containing the deleted entry or an error.</returns>
        protected abstract Result<IEntry<TValue>> DeleteStorage(long id);

        /// <summary>
        /// Deletes all entries from the cache storage.
        /// </summary>
        /// <returns>A result indicating success or failure.</returns>
        protected abstract Result DeleteAll();

        /// <summary>
        /// Gets the current count of entries in the cache.
        /// </summary>
        /// <returns>A result containing the count or an error.</returns>
        protected abstract Result<long> GetCurrentCount();

        /// <summary>
        /// Performs custom disposal logic for derived classes.
        /// </summary>
        protected abstract void DisposeInternal();

        /// <summary>
        /// Updates or inserts metadata for cache entries.
        /// </summary>
        /// <param name="metadata">The metadata to upsert.</param>
        /// <returns>A result indicating success or failure.</returns>
        protected abstract Result Upsert(IEnumerable<Metadata> metadata);

        protected abstract Result<IEntry<TValue>> UpdateInternal(long id, TValue value);

        /// <summary>
        /// Gets the metadata for the head (first) entry in the cache.
        /// </summary>
        /// <returns>A result containing the head metadata or an error.</returns>
        protected abstract Result<Metadata> GetCurrentHead();

        /// <summary>
        /// Gets the metadata for the tail (last) entry in the cache.
        /// </summary>
        /// <returns>A result containing the tail metadata or an error.</returns>
        protected abstract Result<Metadata> GetCurrentTail();

        /// <summary>
        /// Gets the metadata for a specific entry by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entry.</param>
        /// <returns>A result containing the metadata or an error.</returns>
        protected abstract Result<Metadata> GetMetadata(long id);

        /// <summary>
        /// Deletes metadata for a specific entry by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the metadata to delete.</param>
        /// <returns>A result indicating success or failure.</returns>
        protected abstract Result DeleteMetadata(long id);

        /// <summary>
        /// Deletes all metadata from the cache.
        /// </summary>
        /// <returns>A result indicating success or failure.</returns>
        protected abstract Result DeleteAllMetadata();

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Add(TValue value)
        {
            Locker.EnterWriteLock();
            try { return Insert(value).Bind(entry => AddTail(entry)).Bind(entry => AddToL1Store(entry).Bind(() => SignalAwaiters().Bind(() => Result.Ok(entry)))); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result AddToL1Store(IEntry<TValue> entry)
        {
            return L1StoreCount >= L1StoreCurrentCap ? Result.Ok() : Result.Try(() => _l1Store.AddLast(entry))
                                                                           .Bind(node => Result.Try(() => l1Lookup.Add(entry.Id, node)));
        }

        public Result<IEntry<TValue>> Update(long id, TValue value)
        {
            Locker.EnterWriteLock();
            try { return Fetch(id).Bind(entry => UpdateInternal(id, value)).Bind(entry => UpdateL1Store(entry).Bind(() => Result.Ok(entry))); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result UpdateL1Store(IEntry<TValue> entry)
        {
            return l1Lookup.TryGetValueOrDefault<Dictionary<long, LinkedListNode<IEntry<TValue>>>, long, LinkedListNode<IEntry<TValue>>>(entry.Id)
                           .Bind(node => node == null ? Result.Ok() : Result.Try(() => { node.Value = entry; }));
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Get(long id)
        {
            Locker.EnterReadLock();
            try { return TryGetFromL1Store(id).Bind(entry => entry == null ? Fetch(id) : Result.Ok(entry)); }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> TryGetFromL1Store(long id)
        {
            return l1Lookup.TryGetValueOrDefault<Dictionary<long, LinkedListNode<IEntry<TValue>>>, long, LinkedListNode<IEntry<TValue>>>(id)
                           .Bind(node => Result.Ok(node?.Value));
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> GetFirst()
        {
            Locker.EnterReadLock();
            try { return GetFirstInternal()!; }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> GetFirstInternal()
        {
            return TryGetFirstFromL1Store().Bind(first => first != null ? Result.Ok(first) : GetCurrentCount().Bind(count => count > 0 ? GetCurrentHead().Bind(metadata => Fetch(metadata.Id)) : Result.Ok<IEntry<TValue>>(null!)))!;
        }

        private Result<IEntry<TValue>?> TryGetFirstFromL1Store()
        {
            return Result.Ok<IEntry<TValue>?>(_l1Store?.First?.Value);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> GetLast()
        {
            Locker.EnterReadLock();
            try { return TryGetLastFromL1Store().Bind(last => last != null ? Result.Ok(last) : GetCurrentCount().Bind(count => count > 0 ? GetCurrentTail().Bind(metadata => Fetch(metadata.Id)) : Result.Ok<IEntry<TValue>>(null))); }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> TryGetLastFromL1Store()
        {
            return Result.Ok<IEntry<TValue>?>(_l1Store?.Last?.Value);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Remove(long id)
        {
            Locker.EnterWriteLock();
            try { return DeleteStorage(id).Bind(entry => RemoveMetadata(entry.Id).Bind(() => RemoveFromL1Store(id).Bind(() => Result.Ok(entry)))); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result RemoveFromL1Store(long id)
        {
            return l1Lookup.TryGetValueOrDefault<Dictionary<long, LinkedListNode<IEntry<TValue>>>, long, LinkedListNode<IEntry<TValue>>>(id)
                           .Bind(node => node == null ? Result.Ok() : Result.Try(() => _l1Store.Remove(node))
                                                                            .Bind(() => Result.Try(() => l1Lookup.Remove(id))
                                                                                              .Bind(removeResult => removeResult ? Result.Ok() : Result.Fail(""))))
                           .Bind(() => ReplenishL1Store());
        }

        private Result ReplenishL1Store()
        {
            var fetchNextFromL2Result = L1StoreCount == 0 ? GetFirstInternal() : FetchNext(_l1Store.Last.Value.Id);
            while (L1StoreCount < L1StoreCurrentCap && fetchNextFromL2Result.ValueOrDefault != null)
            {
                fetchNextFromL2Result = AddToL1Store(fetchNextFromL2Result.Value).Bind(() => FetchNext(_l1Store.Last.Value.Id));
            }
            return fetchNextFromL2Result.Bind(_ => Result.Ok());
        }

        /// <inheritdoc/>
        public Result Clear()
        {
            Locker.EnterWriteLock();
            try{ return ClearInternal(); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result ClearInternal()
        {
            return DeleteAll().Bind(() => DeleteAllMetadata()).Bind(() => ClearL1Store());
        }

        public Result ClearL1Store()
        {
            return Result.Try(() => _l1Store.Clear()).Bind(() => Result.Try(() => l1Lookup.Clear()));
        }

        /// <inheritdoc/>
        public Result<long> Count()
        {
            Locker.EnterReadLock();
            try { return GetCurrentCount(); }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public Task<Result<IEntry<TValue>>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default)
        {
            Locker.EnterReadLock();
            try
            {
                var getHeadResult = GetFirstInternal();
                if (id == null)
                {
                    if (getHeadResult.ValueOrDefault == null) //there is no data in the cache
                    {
                        return GetNextGenAwaiter(cancellationToken).Bind(task => Result.Try(() => task)).Bind(_ => Task.FromResult(GetFirst()));
                    }
                    else
                    {
                        return Task.FromResult(getHeadResult);
                    }
                }
                else if (getHeadResult?.ValueOrDefault?.Id > id)
                {
                    return Task.FromResult(getHeadResult);
                }
                else
                {
                    return FetchNext(id.Value).Bind(nextEntry => nextEntry == null ? 
                                                                 GetNextGenAwaiter(cancellationToken).Bind(task => Result.Try(() => task)).Bind(_ => Task.FromResult(Get(id.Value))) : 
                                                                 Task.FromResult(Result.Ok(nextEntry)));
                }
            }
            finally { Locker.ExitReadLock(); }
        }

        private Result<Task<bool>> GetNextGenAwaiter(CancellationToken cancellationToken = default)
        {
            return Result.Try(() => areReadersWaiting = true)
                         .Bind(_ => nextGenAwaiter.RegisterCancellationToken(cancellationToken))
                         .Bind(() => Result.Ok(nextGenAwaiter.Task));
        }

        private Result SignalAwaiters()
        {
            return areReadersWaiting ?
                   Result.Try(() =>
                   {
                       var prevGenAwaiter = nextGenAwaiter;
                       nextGenAwaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                       areReadersWaiting = false;
                       prevGenAwaiter.TrySetResult(true);
                   }) : 
                   Result.Ok();
        }

        /// <summary>
        /// Adds a new tail entry to the cache metadata.
        /// </summary>
        /// <param name="entry">The entry to add as the tail.</param>
        /// <returns>A result containing the entry or an error.</returns>
        private Result<IEntry<TValue>> AddTail(IEntry<TValue> entry)
        {
            return Result.Try(() => new Metadata { Id = entry.Id })
                         .Bind(newTail => GetCurrentTail().Bind(currentTail => AddTail(currentTail, newTail).Bind(() => Upsert(currentTail == null ? [newTail] : [currentTail, newTail]))))
                         .Bind(() => Result.Ok(entry));
        }

        /// <summary>
        /// Updates the tail and previous pointers in the cache metadata.
        /// </summary>
        /// <param name="currentTail">The current tail metadata.</param>
        /// <param name="newTail">The new tail metadata.</param>
        /// <returns>A result indicating success or failure.</returns>
        private Result AddTail(Metadata currentTail, Metadata newTail)
        {
            return currentTail == null ? 
                   Result.Ok() : 
                   Result.Try(() => currentTail.Next = newTail.Id)
                         .Bind(_ => Result.Try(() => newTail.Previous = currentTail.Id))
                         .Bind(_ => Result.Ok());
        }

        /// <summary>
        /// Removes metadata for a specific entry and updates adjacent entries.
        /// </summary>
        /// <param name="id">The unique identifier of the entry to remove.</param>
        /// <returns>A result indicating success or failure.</returns>
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Locker.EnterWriteLock();
                    try
                    {
                        ClearInternal();
                        nextGenAwaiter.TrySetCanceled();
                        areReadersWaiting = false;
                        DisposeInternal();
                    }
                    finally { Locker.ExitWriteLock(); }                    
                    Locker.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
