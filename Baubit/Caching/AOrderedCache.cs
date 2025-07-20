using Baubit.Tasks;
using FluentResults;
using FluentResults.Extensions;
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
            try { return Insert(value).Bind(entry => nextSignal.Set(entry.Id).Bind(() => Result.Ok(entry))); }
            finally { Locker.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Get(long id)
        {
            Locker.EnterReadLock();
            try { return Fetch(id); }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Remove(long id)
        {
            Locker.EnterWriteLock();
            try { return DeleteStorage(id).Bind(entry => RemoveMetadata(entry.Id).Bind(() => Result.Ok(entry))); }
            finally { Locker.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public Result Clear()
        {
            Locker.EnterWriteLock();
            try{ return DeleteAll().Bind(() => DeleteAllMetadata()); }
            finally { Locker.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public Result<long> Count()
        {
            Locker.EnterReadLock();
            try { return GetCurrentCount(); }
            finally { Locker.ExitReadLock(); }
        }

        /// <summary>
        /// Signal used to notify when a new entry is added to the cache.
        /// </summary>
        ManualResetTask<long> nextSignal = new ManualResetTask<long>();

        /// <inheritdoc/>
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

        /// <summary>
        /// Waits asynchronously for the next entry to be added to the cache.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing the result of the next entry.</returns>
        private Task<Result<IEntry<TValue>>> AwaitNextAsync(CancellationToken cancellationToken = default)
        {
            return nextSignal.Reset(cancellationToken).Bind(() => nextSignal.WaitAsync().Bind(nextId => Fetch(nextId)));
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

        /// <summary>
        /// Releases resources used by the cache.
        /// </summary>
        public void Dispose()
        {
            Locker.Dispose();
            DisposeInternal();
        }
    }
}
