using Baubit.Caching.InMemory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Reflection.Emit;
using System.Threading;

namespace Baubit.Caching.Redis
{
    public class Metadata : IMetadata
    {
        public long Count { get => GetCountInternal(); }

        public Guid? HeadId { get => GetHeadIdInternal(); }
        public Guid? TailId { get => GetTailIdInternal(); }

        private IMetadata _internal;
        private IDatabase _database;
        private ILogger<Metadata> _logger;
        private CancellationTokenSource _syncerCTS = new CancellationTokenSource();
        private Task<bool> _syncer;
        private ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private SynchronizationOptions _synchronizationOptions;

        private DistributedLock? _distributedLock;
        public Metadata(SynchronizationOptions synchronizationOptions,
                        IMetadata @internal,
                        IDatabase database,
                        ILoggerFactory loggerFactory)
        {
            _synchronizationOptions = synchronizationOptions;
            _internal = @internal;
            _database = database;
            _logger = loggerFactory.CreateLogger<Metadata>();
            FetchKeys();
            InitializeConsumerGroup();
            _syncer = BeginSyncAsync(_syncerCTS.Token);
        }

        private void FetchKeys()
        {
            foreach (var key in _database.GetSetKeys().Order())
            {
                AddTailInternal(key);
            }
        }

        private void InitializeConsumerGroup()
        {
            try
            {
                _database.StreamCreateConsumerGroup(_synchronizationOptions.StreamKey,
                                                    _synchronizationOptions.GroupName,
                                                    StreamPosition.NewMessages,
                                                    createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP")) { /* ok */ }
        }

        private async Task<bool> BeginSyncAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var entries = _database.StreamReadGroup(_synchronizationOptions.StreamKey,
                                                        _synchronizationOptions.GroupName,
                                                        _synchronizationOptions.ConsumerName,
                                                        ">", // read new messages
                                                        count: 128);

                if (entries is null || entries.Length == 0)
                {
                    await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                ProcessEvents(entries.Parse(_synchronizationOptions.ConsumerName));

                _database.StreamAcknowledge(_synchronizationOptions.StreamKey,
                                            _synchronizationOptions.GroupName,
                                            entries.Select(e => e.Id).ToArray());
            }
            return true;
        }

        private void ProcessEvents(IEnumerable<EventDescriptor> descriptors)
        {
            _locker.EnterWriteLock();
            try
            {
                foreach (var descriptor in descriptors)
                {
                    switch (descriptor.EventType)
                    {
                        case EventType.Create:
                            AddTailInternal(descriptor.MetadataId);
                            break;
                        case EventType.Delete:
                            Remove(descriptor.MetadataId);
                            break;
                    }
                }
            }
            finally { _locker.ExitWriteLock(); }
        }

        public bool AddTail(Guid id)
        {
            _locker.EnterWriteLock();
            try
            {
                var retVal = AddTailInternal(id);

                if (retVal)
                {
                    _database.StreamAdd(_synchronizationOptions.StreamKey, new[]
                    {
                        new NameValueEntry(nameof(EventDescriptor.Source), _synchronizationOptions.ConsumerName),
                        new NameValueEntry(nameof(EventDescriptor.EventType), EventType.Create.ToString()),
                        new NameValueEntry(nameof(EventDescriptor.EventId), Guid.NewGuid().ToString("N")),
                        new NameValueEntry(nameof(EventDescriptor.MetadataId), id.ToString())
                    });
                }
                return retVal;
            }
            finally
            {
                SetGlobalTailIdInternal(id);
                _distributedLock?.Dispose();
                _distributedLock = null;
                _locker.ExitWriteLock();
            }
        }

        private bool AddTailInternal(Guid id)
        {
            return _internal.AddTail(id);
        }

        public bool ContainsKey(Guid id)
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.ContainsKey(id);
            }
            finally { _locker.ExitReadLock(); }
        }

        public bool GetIdsThrough(Guid id, out IEnumerable<Guid> ids)
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.GetIdsThrough(id, out ids);
            }
            finally { _locker.ExitReadLock(); }
        }

        public bool GetNextId(Guid? id, out Guid? nextId)
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.GetNextId(id, out nextId);
            }
            finally { _locker.ExitReadLock(); }
        }

        public bool GenerateNextId(out Guid nextId)
        {
            nextId = default;

            // take the lock here and release it after adding the id to the store
            // this will ensure order is maintained throughout the system
            _distributedLock = DistributedLock.Take(_database, _synchronizationOptions.LockKey, _synchronizationOptions.IdSeedLockTtl);

            while (GetGlobalTailIdInternal() > TailId) 
            {
                // local tail is lagging.
                // Allow synchronizer to run and update the the local tail.
                // The id generator can then generate an id that is after the global tail.
                Thread.Sleep(150);
            }

            return _internal.GenerateNextId(out nextId);
        }

        public bool Remove(Guid id)
        {
            _locker.EnterWriteLock();
            try
            {
                var retVal = _internal.Remove(id);

                if (retVal)
                {
                    _database.StreamAdd(_synchronizationOptions.StreamKey, new[]
                    {
                        new NameValueEntry(nameof(EventDescriptor.Source), _synchronizationOptions.ConsumerName),
                        new NameValueEntry(nameof(EventDescriptor.EventType), EventType.Delete.ToString()),
                        new NameValueEntry(nameof(EventDescriptor.EventId), Guid.NewGuid().ToString("N")),
                        new NameValueEntry(nameof(EventDescriptor.MetadataId), id.ToString())
                    });
                }
                return retVal;
            }
            finally { _locker.ExitWriteLock(); }
        }

        public long ResetRoomCount()
        {
            return _internal.ResetRoomCount();
        }

        public Task<Guid> GetNextIdAsync(Guid? id, CancellationToken cancellationToken)
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.GetNextIdAsync(id, cancellationToken);
            }
            finally { _locker.ExitReadLock(); }
        }

        private Guid? GetHeadIdInternal()
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.HeadId;
            }
            finally { _locker.ExitReadLock(); }
        }

        private Guid? GetTailIdInternal()
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.TailId;
            }
            finally { _locker.ExitReadLock(); }
        }

        private Guid? GetGlobalTailIdInternal()
        {
            Guid? globalTailId = null;
            var tailRaw = _database.StringGet(_synchronizationOptions.GlobalTailIdKey);

            if (tailRaw.HasValue && Guid.TryParse(tailRaw.ToString(), out var parsed)) globalTailId = parsed;
            return globalTailId;
        }

        private bool SetGlobalTailIdInternal(Guid newId)
        {
            return _database.StringSet(_synchronizationOptions.GlobalTailIdKey, newId.ToString("N"));
        }

        private long GetCountInternal()
        {
            _locker.EnterReadLock();
            try
            {
                return _internal.Count;
            }
            finally { _locker.ExitReadLock(); }
        }
    }

    public class SynchronizationOptions
    {
        public string StreamKey { get; init; }
        public string GroupName { get; init; }
        public string ConsumerName { get; init; }
        public string LockKey { get; init; }
        public string GlobalTailIdKey { get; init; }
        public int IdSeedLockTtlMs { get; init; } = 5000; // 5 second default
        public TimeSpan IdSeedLockTtl => TimeSpan.FromMilliseconds(IdSeedLockTtlMs);
    }

    public record EventDescriptor(string Source, EventType EventType, Guid EventId, Guid MetadataId);

    public static class StreamEntryExtensions
    {
        public static IEnumerable<EventDescriptor> Parse(this IEnumerable<StreamEntry> entries, string ignoreSource)
        {
            foreach (var entry in entries)
            {
                if (entry.TryParse(ignoreSource, out var descriptor)) yield return descriptor;
            }
        }
        public static bool TryParse(this StreamEntry entry, string ignoreSource, out EventDescriptor? descriptor)
        {
            var source = string.Empty;
            var eventType = EventType.None;
            var eventId = Guid.NewGuid();
            var metadataId = Guid.NewGuid();
            foreach (var value in entry.Values)
            {
                switch (value.Name)
                {
                    case nameof(EventDescriptor.Source):
                        source = value.Value;
                        break;
                    case nameof(EventDescriptor.EventType):
                        Enum.TryParse<EventType>(value.Value, out eventType);
                        break;
                    case nameof(EventDescriptor.EventId):
                        Guid.TryParse(value.Value, out eventId);
                        break;
                    case nameof(EventDescriptor.MetadataId):
                        Guid.TryParse(value.Value, out metadataId);
                        break;
                }
            }
            if (source == ignoreSource) { descriptor = null; return false; }
            descriptor = new EventDescriptor(source, eventType, eventId, metadataId);
            return true;
        }
        public static IEnumerable<Guid> GetSetKeys(this IDatabase database, int pageSize = 1000)
        {
            var cursor = "0";
            do
            {
                var res = (RedisResult[])database.Execute("SCAN", cursor, "TYPE", "set", "COUNT", pageSize);

                cursor = (string)res[0];

                foreach (var k in (RedisResult[])res[1]) yield return Guid.Parse(k.ToString());

            } while (cursor != "0");
        }
    }

    public enum EventType
    {
        None,
        Create,
        Delete
    }
}
