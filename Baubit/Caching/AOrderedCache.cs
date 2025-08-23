using Baubit.Collections;
using Baubit.States;
using Baubit.Tasks;
using Baubit.Traceability;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    /// <summary>
    /// Provides a base implementation of <see cref="IOrderedCache{TValue}"/> with
    /// thread-safe operations, in-memory L1 storage, and pluggable L2 persistence.
    /// </summary>
    /// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
    public abstract class AOrderedCache<TValue> : IOrderedCache<TValue>
    {
        /// <summary>
        /// Gets the cache configuration values.
        /// </summary>
        public Configuration Configuration { get; init; }

        /// <summary>
        /// Gets the current capacity of the L1 store.<br/>
        /// This may grow or shrink adaptively if enabled.
        /// </summary>
        public int L1StoreCurrentCap { get => _l1StoreCurrentCap; private set => _l1StoreCurrentCap = value; }

        /// <summary>
        /// Gets the current number of entries in the L1 store.
        /// </summary>
        public int L1StoreCount => _l1Store.Count;

        /// <summary>
        /// Synchronizes access to cache operations for thread safety.
        /// </summary>
        protected readonly ReaderWriterLockSlim Locker = new();

        #region PrivateMembers
        private bool disposedValue;

        private long _gateCount;
        private volatile int _l1StoreCurrentCap;
        private LinkedList<IEntry<TValue>> _l1Store = new LinkedList<IEntry<TValue>>();
        private Dictionary<long, LinkedListNode<IEntry<TValue>>> l1Lookup = new Dictionary<long, LinkedListNode<IEntry<TValue>>>();
        private volatile bool areReadersWaiting = false;
        private TaskCompletionSource<bool> nextGenAwaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private Task<Result> adaptionRunner;
        private CancellationTokenSource adaptionCTS;
        private readonly ILogger<AOrderedCache<TValue>> _logger;
        #endregion

        protected AOrderedCache(Configuration cacheConfiguration,
                                ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AOrderedCache<TValue>>();
            Configuration = cacheConfiguration;
            L1StoreCurrentCap = Configuration.L1StoreInitialCap;
            if (Configuration.RunAdaptiveResizing)
            {
                adaptionCTS = new CancellationTokenSource();
                adaptionRunner = RunAdaptiveResizing(adaptionCTS.Token);
            }
        }

        #region AdaptiveResizing
        private async Task<Result> RunAdaptiveResizing(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(Configuration.AdaptionWindowMS, cancellationToken);
                    _logger.LogDebug($"Gates this cycle: {_gateCount}");
                    var gatesThisCycle = Interlocked.Exchange(ref _gateCount, 0);

                    double gateRate = gatesThisCycle * 1_000.0 / Configuration.AdaptionWindowMS;

                    _logger.LogTrace($"Gate rate: {gateRate}");

                    int? newCap = null;
                    if (gateRate > Configuration.GateRateUpperLimit && L1StoreCurrentCap < Configuration.MaxCap)
                    {
                        newCap = Math.Min(Configuration.MaxCap, _l1StoreCurrentCap + Configuration.GrowStep);
                    }
                    else if (gateRate < Configuration.GateRateLowerLimit && L1StoreCurrentCap > Configuration.MinCap)
                    {
                        newCap = Math.Max(Configuration.MinCap, _l1StoreCurrentCap - Configuration.ShrinkStep);
                    }
                    if (newCap != null)
                    {
                        ResizeL1Store(newCap.Value).ThrowIfFailed();
                        _logger.LogTrace($"Resized L1Store. New size: {L1StoreCurrentCap}");
                    }
                }
                return Result.Ok();
            }
            catch(TaskCanceledException)
            {
                return Result.Ok();
            }
            catch(Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private Result ResizeL1Store(int newCapacity)
        {
            Locker.EnterWriteLock();
            try { return Result.Try(() => Interlocked.Exchange(ref _l1StoreCurrentCap, newCapacity)).Bind(_ => ReplenishL1Store()); }
            finally { Locker.ExitWriteLock(); }
        }
        #endregion

        #region AbstractMethods
        /// <summary>
        /// Inserts a value into the L2 store.
        /// </summary>
        protected abstract Result<IEntry<TValue>> AddToL2Store(TValue value);

        /// <summary>
        /// Retrieves an entry by identifier from the L2 store.
        /// </summary>
        protected abstract Result<IEntry<TValue>> GetFromL2Store(long id);

        /// <summary>
        /// Retrieves the entry following the specified identifier from the L2 store.
        /// </summary>
        protected abstract Result<IEntry<TValue>> GetNextFromL2Store(long id);

        /// <summary>
        /// Deletes an entry by identifier from the L2 store.
        /// </summary>
        protected abstract Result<IEntry<TValue>> DeleteFromL2Store(long id);

        /// <summary>
        /// Deletes all entries from the L2 store.
        /// </summary>
        protected abstract Result ClearL2Store();

        /// <summary>
        /// Returns the count of entries in the L2 store.
        /// </summary>
        protected abstract Result<long> GetL2StoreCount();

        /// <summary>
        /// Releases resources owned by the L2 store implementation.
        /// </summary>
        protected abstract void DisposeL2StoreResources();

        /// <summary>
        /// Creates or updates metadata entries in the L2 store.
        /// </summary>
        protected abstract Result UpsertL2Store(IEnumerable<Metadata> metadata);

        /// <summary>
        /// Updates an existing L2 entry.
        /// </summary>
        protected abstract Result<IEntry<TValue>> UpdateL2Store(long id, TValue value);

        /// <summary>
        /// Retrieves the metadata for the head entry in L2.
        /// </summary>
        protected abstract Result<Metadata> GetCurrentHead();

        /// <summary>
        /// Retrieves the metadata for the tail entry in L2.
        /// </summary>
        protected abstract Result<Metadata> GetCurrentTail();

        /// <summary>
        /// Retrieves metadata for a given entry identifier.
        /// </summary>
        protected abstract Result<Metadata> GetMetadata(long id);

        /// <summary>
        /// Deletes metadata by entry identifier.
        /// </summary>
        protected abstract Result DeleteMetadata(long id);

        /// <summary>
        /// Clears all metadata records from the L2 store.
        /// </summary>
        protected abstract Result ClearMetadata();

        #endregion

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Add(TValue value)
        {
            Locker.EnterWriteLock();
            try { return AddToL2Store(value).Bind(entry => AddTail(entry)).Bind(entry => AddToL1Store(entry).Bind(() => SignalAwaiters().Bind(() => Result.Ok(entry)))); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result AddToL1Store(IEntry<TValue> entry)
        {
            return L1StoreCount >= L1StoreCurrentCap ? Result.Ok() : Result.Try(() => _l1Store.AddLast(entry))
                                                                           .Bind(node => Result.Try(() => l1Lookup.Add(entry.Id, node)));
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Update(long id, TValue value)
        {
            Locker.EnterWriteLock();
            try { return GetFromL2Store(id).Bind(entry => UpdateL2Store(id, value)).Bind(entry => UpdateL1Store(entry).Bind(() => Result.Ok(entry))); }
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
            try { return GetInternal(id); }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>> GetInternal(long id)
        {
            return TryGetFromL1Store(id).Bind(entry => entry == null ? GetFromL2Store(id) : Result.Ok(entry));
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> GetNext(long? id)
        {
            Locker.EnterReadLock();
            try
            {
                var getFirstResult = GetFirstInternal();
                if (id == null) return getFirstResult;
                else if (id.Value < getFirstResult?.ValueOrDefault?.Id) return getFirstResult;
                else return GetNextInternal(id.Value);
            }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> GetNextInternal(long id)
        {
            return TryGetNextFromL1Store(id).Bind(entry => entry == null ? GetNextFromL2Store(id) : Result.Ok(entry));
        }

        private Result<IEntry<TValue>?> TryGetFromL1Store(long id)
        {
            return l1Lookup.TryGetValueOrDefault<Dictionary<long, LinkedListNode<IEntry<TValue>>>, long, LinkedListNode<IEntry<TValue>>>(id)
                           .Bind(node => Result.Ok(node?.Value));
        }

        private Result<IEntry<TValue>?> TryGetNextFromL1Store(long id)
        {
            return l1Lookup.TryGetValueOrDefault<Dictionary<long, LinkedListNode<IEntry<TValue>>>, long, LinkedListNode<IEntry<TValue>>>(id)
                           .Bind(node => Result.Ok(node?.Next?.Value));
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
            return TryGetFirstFromL1Store().Bind(first => first != null ? Result.Ok(first) : GetL2StoreCount().Bind(count => count > 0 ? GetCurrentHead().Bind(metadata => GetFromL2Store(metadata.Id)) : Result.Ok<IEntry<TValue>>(null!)))!;
        }

        private Result<IEntry<TValue>?> TryGetFirstFromL1Store()
        {
            return Result.Ok<IEntry<TValue>?>(_l1Store?.First?.Value);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> GetLast()
        {
            Locker.EnterReadLock();
            try { return TryGetLastFromL1Store().Bind(last => last != null ? Result.Ok(last) : GetL2StoreCount().Bind(count => count > 0 ? GetCurrentTail().Bind(metadata => GetFromL2Store(metadata.Id)) : Result.Ok<IEntry<TValue>>(null))); }
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
            try { return GetInternal(id).Bind(entry => entry == null ? Result.Ok() : DeleteFromL2Store(id).Bind(entry => RemoveMetadata(entry.Id).Bind(() => RemoveFromL1Store(id).Bind(() => Result.Ok(entry))))); }
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
            var fetchNextFromL2Result = L1StoreCount == 0 ? GetFirstInternal() : GetNextFromL2Store(_l1Store.Last.Value.Id);
            while (L1StoreCount < L1StoreCurrentCap && fetchNextFromL2Result.ValueOrDefault != null)
            {
                fetchNextFromL2Result = AddToL1Store(fetchNextFromL2Result.Value).Bind(() => GetNextFromL2Store(_l1Store.Last.Value.Id));
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
            return ClearL2Store().Bind(() => ClearMetadata()).Bind(() => ClearL1Store());
        }

        private Result ClearL1Store()
        {
            return Result.Try(() => _l1Store.Clear()).Bind(() => Result.Try(() => l1Lookup.Clear()));
        }

        /// <inheritdoc/>
        public Result<long> Count()
        {
            Locker.EnterReadLock();
            try { return GetL2StoreCount(); }
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
                    return GetNextInternal(id.Value).Bind(nextEntry => nextEntry == null ?
                                                                       GetNextGenAwaiter(cancellationToken).Bind(task => Result.Try(() => task)).Bind(_ => GetNextAsync(id.Value, cancellationToken)) :
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
                       if (Configuration.RunAdaptiveResizing) Interlocked.Increment(ref _gateCount);
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
                         .Bind(newTail => GetCurrentTail().Bind(currentTail => AddTail(currentTail, newTail).Bind(() => UpsertL2Store(currentTail == null ? [newTail] : [currentTail, newTail]))))
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
                   
                   }).Bind(() => upsertables.Count == 0 ? Result.Ok() : UpsertL2Store(upsertables)).Bind(() => current == null ? Result.Ok() : DeleteMetadata(current.Id));
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
                        adaptionCTS?.Cancel();
                        adaptionRunner?.Wait(true);
                        ClearInternal();
                        nextGenAwaiter.TrySetCanceled();
                        areReadersWaiting = false;
                        DisposeL2StoreResources();
                    }
                    finally { Locker.ExitWriteLock(); }                    
                    Locker.Dispose();
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
