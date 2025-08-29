using Baubit.Caching.InMemory;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Logging;
using Baubit.Tasks;

namespace Baubit.Caching
{
    public class OrderedCache<TValue> : IOrderedCache<TValue>
    {
        public Configuration Configuration { get; init; }

        public long Count { get => _metadata.Count; }

        protected readonly ReaderWriterLockSlim Locker = new();

        #region PrivateMembers
        private bool disposedValue;

        private long _roomCount;

        private IMetadata _metadata;

        private WaitingRoom<Result<IEntry<TValue>>> _waitingRoom = new WaitingRoom<Result<IEntry<TValue>>>();
        private Task<Result>? adaptionRunner;
        private CancellationTokenSource? adaptionCTS;
        private readonly ILogger<OrderedCache<TValue>> _logger;

        private IDataStore<TValue>? _l1DataStore;
        private IDataStore<TValue> _l2DataStore;
        #endregion

        public OrderedCache(Configuration cacheConfiguration, 
                            IDataStore<TValue>? l1DataStore, 
                            IDataStore<TValue> l2DataStore,
                            IMetadata metadata,
                            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderedCache<TValue>>();
            Configuration = cacheConfiguration;
            _l1DataStore = l1DataStore;
            _l2DataStore = l2DataStore;
            _metadata = metadata;
            if (_l1DataStore != null && !_l1DataStore.Uncapped && Configuration?.RunAdaptiveResizing == true)
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
                    await Task.Delay(Configuration.AdaptionWindowMS, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug($"Rooms this cycle: {_roomCount}");
                    var roomsThisCycle = Interlocked.Exchange(ref _roomCount, 0);

                    double roomRate = roomsThisCycle * 1_000.0 / Configuration.AdaptionWindowMS;

                    _logger.LogTrace($"Room rate: {roomRate}");

                    if (roomRate > Configuration.RoomRateUpperLimit)
                    {
                        _l1DataStore?.AddCapacity(Configuration.GrowStep);
                        _logger.LogTrace($"Resized L1Store. New size: {_l1DataStore?.TargetCapacity}");
                    }
                    else if (roomRate < Configuration.RoomRateLowerLimit)
                    {
                        _l1DataStore?.CutCapacity(Configuration.ShrinkStep);
                        _logger.LogTrace($"Resized L1Store. New size: {_l1DataStore?.TargetCapacity}");
                    }
                    Locker.EnterWriteLock();
                    try { ReplenishL1Store(); }
                    catch { throw; }
                    finally { Locker.ExitWriteLock(); }
                }
                return Result.Ok();
            }
            catch (TaskCanceledException)
            {
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
        #endregion

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Add(TValue value)
        {
            Locker.EnterWriteLock();
            try
            {

                return CreateNewEntry(value).Bind(entry => AddToL1Store(entry).Bind(() => SignalAwaiters(Result.Ok(entry)).Bind(() => Result.Ok(entry))));
            }
            finally { Locker.ExitWriteLock(); }
        }

        private Result<IEntry<TValue>> CreateNewEntry(TValue value)
        {
            return _l2DataStore.Add(value).Bind(entry => _metadata.AddTail(entry.Id).Bind(() => Result.Ok(entry)));
        }

        private Result AddToL1Store(IEntry<TValue> entry)
        {
            return _l1DataStore == null ? Result.Ok() : _l1DataStore.HasCapacity ? _l1DataStore.Add(entry) : Result.Ok();
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>> Update(long id, TValue value) // TBD - datastore refactor
        {
            Locker.EnterWriteLock();
            try
            {
                return GetEntryOrDefaultInternal(id).Bind(entry => entry == null ? Result.Ok(entry) : _l2DataStore.Update(id, value).Bind(entry => UpdateL1Store(entry).Bind(() => Result.Ok(entry))));
            }
            finally { Locker.ExitWriteLock(); }
        }

        private Result UpdateL1Store(IEntry<TValue> entry)
        {
            return _l1DataStore == null ? Result.Ok() : _l1DataStore.Update(entry).Bind(_ => Result.Ok());
        }

        public Result<IEntry<TValue>?> this[long index] => GetEntryOrDefault(index); // TBD - datastore refactor

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> GetEntryOrDefault(long? id) // TBD - datastore refactor
        {
            Locker.EnterReadLock();
            try { return GetEntryOrDefaultInternal(id); }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> GetEntryOrDefaultInternal(long? id)
        {
            try
            {
                return Result.Ok().Bind(() => _l1DataStore == null ? Result.Ok(default(IEntry<TValue>)) : _l1DataStore.GetEntryOrDefault(id)).Bind(entry => entry == null ? _l2DataStore.GetEntryOrDefault(id) : Result.Ok<IEntry<TValue>?>(entry));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> GetNextOrDefault(long? id) // TBD - datastore refactor
        {
            Locker.EnterReadLock();
            try { return GetNextOrDefaultInternal(id); }
            finally { Locker.ExitReadLock(); }
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> GetNextOrDefaultInternal(long? id) // TBD - datastore refactor
        {
            return _metadata.GetNextId(id).Bind(GetEntryOrDefaultInternal);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> GetFirstOrDefault() // TBD - datastore refactor
        {
            Locker.EnterReadLock();
            try { return GetFirstOrDefaultInternal()!; }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> GetFirstOrDefaultInternal() // TBD - datastore refactor
        {
            return GetEntryOrDefaultInternal(_metadata.HeadId);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> GetLastOrDefault() // TBD - datastore refactor
        {
            Locker.EnterReadLock();
            try { return GetLastOrDefaultInternal(); }
            finally { Locker.ExitReadLock(); }
        }

        private Result<IEntry<TValue>?> GetLastOrDefaultInternal() // TBD - datastore refactor
        {
            return GetEntryOrDefaultInternal(_metadata.TailId);
        }

        /// <inheritdoc/>
        public Result<IEntry<TValue>?> Remove(long id)
        {
            Locker.EnterWriteLock();
            try { return RemoveInternal(id); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result<IEntry<TValue>?> RemoveInternal(long id)
        {
            try
            {
                if (!_metadata.ContainsKey(id)) return Result.Ok();

                return _l2DataStore.Remove(id).Bind(entry => _l1DataStore == null ? Result.Ok<IEntry<TValue>?>(entry) : _l1DataStore.Remove(id).Bind(_ => ReplenishL1Store()));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }


        private Result ReplenishL1Store()
        {
            return Enumerable.Range(0, (int)_l1DataStore!.CurrentCapacity).Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => AddNextToL1Store(_l1DataStore.TailId)));
        }

        private Result AddNextToL1Store(long? id)
        {
            return _metadata.GetNextId(id).Bind(nextId => _l2DataStore.GetEntryOrDefault(nextId)).Bind(nextEntry => Result.Try(() => { if (nextEntry != null) AddToL1Store(nextEntry); }));
        }

        /// <inheritdoc/>
        public Result Clear()
        {
            Locker.EnterWriteLock();
            try { return ClearInternal(); }
            finally { Locker.ExitWriteLock(); }
        }

        private Result ClearInternal()
        {
            return _l2DataStore.Clear().Bind(ClearL1Store).Bind(ClearMetadata);
        }

        private Result ClearL1Store()
        {
            return _l1DataStore == null ? Result.Ok() : _l1DataStore.Clear();
        }

        private Result ClearMetadata()
        {
            return _metadata.Clear();
        }

        /// <inheritdoc/>
        public Task<Result<IEntry<TValue>>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default)
        {
            Locker.EnterReadLock();
            try
            {
                return GetNextOrDefaultInternal(id).Bind(entry => entry == null ? _waitingRoom.Join(cancellationToken) : Task.FromResult(Result.Ok(entry)));
            }
            finally { Locker.ExitReadLock(); }
        }

        private Result SignalAwaiters(Result<IEntry<TValue>> res)
        {
            return _waitingRoom.HasGuests ?
                   Result.Try(() =>
                   {
                       if (Configuration?.RunAdaptiveResizing == true) Interlocked.Increment(ref _roomCount);
                       var prevRoom = _waitingRoom;
                       _waitingRoom = new WaitingRoom<Result<IEntry<TValue>>>();
                       prevRoom.TrySetResult(res);
                   }) :
                   Result.Ok();
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
                        _waitingRoom.TrySetCanceled();
                        _l1DataStore?.Dispose();
                        _l2DataStore?.Dispose();
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
