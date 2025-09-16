using Baubit.Caching.InMemory;
using Baubit.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching
{
    /// <summary>
    /// Default <see cref="IOrderedCache{TValue}"/> implementation that composes an optional bounded L1 store
    /// and a required L2 store, with metadata to maintain ordering. Supports adaptive resizing of the L1 store.
    /// </summary>
    /// <typeparam name="TValue">The element type held in the cache.</typeparam>
    public class OrderedCache<TValue> : IOrderedCache<TValue>
    {
        /// <summary>
        /// Gets the runtime configuration for this cache instance.
        /// </summary>
        public Configuration Configuration { get; init; }

        /// <inheritdoc/>
        public long Count { get => _metadata.Count; }

        /// <summary>
        /// A reader/writer lock guarding mutations and multi-field reads.
        /// </summary>
        protected readonly ReaderWriterLockSlim Locker = new();

        #region PrivateMembers
        private bool disposedValue;

        private long _roomCount;
        private IMetadata _metadata;

        // Coordinates awaiters for the next entry produced.
        private WaitingRoom<IEntry<TValue>> _waitingRoom = new WaitingRoom<IEntry<TValue>>();
        private Task<bool>? adaptionRunner;
        private CancellationTokenSource? adaptionCTS;
        private readonly ILogger<OrderedCache<TValue>> _logger;

        private IStore<TValue>? _l1Store;
        private IStore<TValue> _l2Store;
        #endregion

        /// <summary>
        /// Creates a new <see cref="OrderedCache{TValue}"/>.
        /// </summary>
        /// <param name="cacheConfiguration">The cache configuration.</param>
        /// <param name="l1Store">Optional bounded L1 store (e.g., in-memory) for hot entries.</param>
        /// <param name="l2Store">Backing L2 store that must persist every entry.</param>
        /// <param name="metadata">Metadata that tracks head/tail ids and next-id lookups.</param>
        /// <param name="loggerFactory">Factory to create a logger for diagnostics and tracing.</param>
        public OrderedCache(Configuration cacheConfiguration,
                            IStore<TValue>? l1Store,
                            IStore<TValue> l2Store,
                            IMetadata metadata,
                            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderedCache<TValue>>();
            Configuration = cacheConfiguration;
            _l1Store = l1Store;
            _l2Store = l2Store;
            _metadata = metadata;
            if (_l1Store != null && !_l1Store.Uncapped && Configuration?.RunAdaptiveResizing == true)
            {
                // Start a background loop that adjusts L1 capacity based on production rate.
                adaptionCTS = new CancellationTokenSource();
                adaptionRunner = RunAdaptiveResizing(adaptionCTS.Token);
            }
        }

        #region AdaptiveResizing
        /// <summary>
        /// Periodically adjusts the L1 capacity by measuring the rate at which new entries are produced.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to terminate the loop.</param>
        /// <returns><c>true</c> if the loop exits normally or is canceled; otherwise throws.</returns>
        private async Task<bool> RunAdaptiveResizing(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(Configuration.AdaptionWindowMS, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug($"Rooms this cycle: {_roomCount}");
                    var roomsThisCycle = Interlocked.Exchange(ref _roomCount, 0);

                    double roomRate = roomsThisCycle * 1_000.0 / Configuration.AdaptionWindowMS;

                    _logger.LogTrace($"Room rate: {roomRate}");

                    if (roomRate > Configuration.RoomRateUpperLimit)
                    {
                        _l1Store?.AddCapacity(Configuration.GrowStep);
                        _logger.LogTrace($"Resized L1Store. New size: {_l1Store?.TargetCapacity}");
                    }
                    else if (roomRate < Configuration.RoomRateLowerLimit)
                    {
                        _l1Store?.CutCapacity(Configuration.ShrinkStep);
                        _logger.LogTrace($"Resized L1Store. New size: {_l1Store?.TargetCapacity}");
                    }
                    Locker.EnterWriteLock();
                    try { ReplenishL1Store(); }
                    catch { throw; }
                    finally { Locker.ExitWriteLock(); }
                }
                return true;
            }
            catch (TaskCanceledException)
            {
                return true;
            }
            catch
            {
                throw;
            }
        }
        #endregion

        /// <inheritdoc/>
        public bool Add(TValue value, out IEntry<TValue> entry)
        {
            Locker.EnterWriteLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                if (!_l2Store.Add(value, out entry)) return false;
                if (_l1Store?.HasCapacity == true)
                {
                    if (!_l1Store.Add(entry)) return false;
                }
                if (!_metadata.AddTail(entry.Id)) return false;
                if (!SignalAwaiters(entry)) return false;
                return true;
            }
            finally { Locker.ExitWriteLock(); }
        }

        /// <summary>
        /// Notifies any waiters created by <see cref="GetNextAsync(long?, CancellationToken)"/> that a new entry is available.
        /// Also feeds the adaptive-resizing counters when enabled.
        /// </summary>
        /// <param name="entry">The entry that was just appended.</param>
        /// <returns><c>true</c> if signals were delivered successfully; otherwise <c>false</c>.</returns>
        private bool SignalAwaiters(IEntry<TValue> entry)
        {
            if (!_waitingRoom.HasGuests) return true;
            if (Configuration?.RunAdaptiveResizing == true) Interlocked.Increment(ref _roomCount);
            var prevRoom = _waitingRoom;
            _waitingRoom = new WaitingRoom<IEntry<TValue>>();
            return prevRoom.TrySetResult(entry);
        }

        /// <inheritdoc/>
        public bool Update(long id, TValue value)
        {
            Locker.EnterWriteLock();
            try
            {
                if (disposedValue) { return false; }
                return _l2Store.Update(id, value) && _l1Store == null ? true : _l1Store.Update(id, value);
            }
            finally { Locker.ExitWriteLock(); }
        }

        /// <inheritdoc/>
        public bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry)
        {
            Locker.EnterReadLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                return GetEntryOrDefaultInternal(id, out entry);
            }
            finally { Locker.ExitReadLock(); }
        }

        private bool GetEntryOrDefaultInternal(long? id, out IEntry<TValue>? entry)
        {
            entry = default;
            if (id.HasValue && _metadata.ContainsKey(id.Value))
            {
                if (_l1Store?.GetEntryOrDefault(id, out entry) == true)
                {
                    return true;
                }
                else if (_l2Store.GetEntryOrDefault(id, out entry))
                {
                    return true;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public bool GetNextOrDefault(long? id, out IEntry<TValue>? entry)
        {
            Locker.EnterReadLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                return GetNextOrDefaultInternal(id, out entry);
            }
            finally { Locker.ExitReadLock(); }
        }

        private bool GetNextOrDefaultInternal(long? id, out IEntry<TValue>? entry)
        {
            entry = default;
            return _metadata.GetNextId(id, out var nextId) && GetEntryOrDefaultInternal(nextId, out entry);

        }

        /// <inheritdoc/>
        public bool GetFirstOrDefault(out IEntry<TValue>? entry)
        {
            Locker.EnterReadLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                entry = default;
                return GetEntryOrDefaultInternal(_metadata.HeadId, out entry);
            }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public bool GetLastOrDefault(out IEntry<TValue>? entry)
        {
            Locker.EnterReadLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                entry = default;
                return GetEntryOrDefaultInternal(_metadata.TailId, out entry);
            }
            finally { Locker.ExitReadLock(); }
        }

        private long? mostRecentWaitingId;

        /// <inheritdoc/>
        public Task<IEntry<TValue>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default)
        {
            Locker.EnterReadLock();
            try
            {
                if (disposedValue) { Task.FromCanceled(cancellationToken); }
                if (GetNextOrDefaultInternal(id, out var entry) && entry != null)
                {
                    return Task.FromResult(entry);
                }
                else
                {
                    mostRecentWaitingId = id;
                    return _waitingRoom.Join(cancellationToken);
                }
            }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public bool Remove(long id, out IEntry<TValue>? entry)
        {
            Locker.EnterWriteLock();
            try
            {
                if (disposedValue) { entry = default; return false; }
                entry = null;
                if (!_l2Store.Remove(id, out var l2Entry)) return false;
                if (_l1Store?.GetEntryOrDefault(id, out var l1Entry) == true && l1Entry != null)
                {
                    if (!_l1Store.Remove(id, out l1Entry)) return false;
                }
                if (!_metadata.Remove(id)) return false;
                if (!ReplenishL1Store()) return false;
                entry = l2Entry;
                return true;
            }
            finally { Locker.ExitWriteLock(); }
        }

        /// <summary>
        /// Fills the L1 store from L2 until either L1 reaches its capacity or there are no more
        /// entries between the L1 tail and the global tail.
        /// </summary>
        /// <returns><c>true</c> always; the method is best‑effort.</returns>
        private bool ReplenishL1Store()
        {
            while (_l1Store?.CurrentCapacity > 0 && 
                   _metadata.GetNextId(_l1Store.TailId, out var nextId) && 
                   _l2Store.GetEntryOrDefault(nextId, out var nextEntry) && 
                   nextEntry != null && _l1Store.Add(nextEntry)) ;
            return true;
        }

        /// <inheritdoc/>
        public bool Clear()
        {
            Locker.EnterWriteLock();
            try
            {
                if (disposedValue) { return false; }
                return _l2Store.Clear() && _l1Store == null ? true : _l1Store.Clear() && _metadata.Clear();
            }
            finally { Locker.ExitWriteLock(); }
        }

        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">When <c>true</c>, called from <see cref="Dispose()"/>; otherwise from the finalizer.</param>
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
                        _ = _l2Store.Clear() && _l1Store == null ? true : _l1Store.Clear() && _metadata.Clear();
                        _waitingRoom.TrySetCanceled();
                        _l1Store?.Dispose();
                        _l2Store?.Dispose();
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
