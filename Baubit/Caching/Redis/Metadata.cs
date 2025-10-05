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
        private ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private RedisSettings _redisSettings;

        private DistributedLock? _distributedLock;
        private Thread synchronizer;
        private bool disposedValue;
        private Guid? synchronizeAfter;

        public Metadata(RedisSettings redisSettings,
                        IMetadata @internal,
                        IDatabase database,
                        ILoggerFactory loggerFactory)
        {
            _redisSettings = redisSettings;
            _internal = @internal;
            _database = database;
            _logger = loggerFactory.CreateLogger<Metadata>();

            // Take the lock here to keep others from adding into the database
            // This will ensure wqe dont miss any items between FetchKeys and ReadeStreamEntries
            _distributedLock = DistributedLock.Take(_database, _redisSettings.LockKey, _redisSettings.IdSeedLockTtl);
            if (redisSettings.ResumeSession)
            {
                // if ResumeSession is enabled, keys starting from the last know headId will be loaded.
                FetchKeys();
            }
            else
            {
                // every session is concerned with items
                // that were generated in its own lifetime
                SetInstanceHeadIdInternal(null);
            }
            InitializeConsumerGroup();

            // this is to ensure we are registered with redis for stream events
            ReadStreamEntries();
            synchronizeAfter = GetGlobalTailIdInternal();
            // release the lock.
            // Any additions after this are ensured to arrive to this node.
            _distributedLock.Dispose();
            _distributedLock = null;

            synchronizer = new Thread(() => BeginSync(_syncerCTS.Token))
            {
                Name = $"RedisSyncer_{_redisSettings.ConsumerName}",
                Priority = ThreadPriority.Highest
            };

            synchronizer.Start();
            //_syncer = BeginSyncAsync(_syncerCTS.Token);
        }

        private void FetchKeys()
        {
            var instanceHeadId = GetInstanceHeadIdInternal();
            foreach (var key in _database.GetSetKeys(_redisSettings).Order())
            {
                if (key < instanceHeadId) continue;
                AddTailInternal(key);
            }
        }

        private void InitializeConsumerGroup()
        {
            try
            {
                _database.StreamCreateConsumerGroup(_redisSettings.StreamKey,
                                                    _redisSettings.ConsumerGroupKey,
                                                    StreamPosition.NewMessages,
                                                    createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP")) { /* ok */ }
        }

        private void BeginSync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var entries = ReadStreamEntries();

                if (entries is null || entries.Length == 0)
                {
                    Thread.Sleep(150);
                    continue;
                }

                // we want to process events sent by peers only
                // Any events sent by us will also show up here. we want to ignore them
                // Any events having this consumer as a destination (includes broadcast events) are the ones we care about
                ProcessEvents(entries.Parse(_redisSettings.ConsumerName, _redisSettings.ConsumerName));

                _database.StreamAcknowledge(_redisSettings.StreamKey,
                                            _redisSettings.ConsumerGroupKey,
                                            entries.Select(e => e.Id).ToArray());
            }
        }

        private StreamEntry[] ReadStreamEntries()
        {
            return _database.StreamReadGroup(_redisSettings.StreamKey,
                                             _redisSettings.ConsumerGroupKey,
                                             _redisSettings.ConsumerName,
                                             ">", // read new messages
                                             count: 128);
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
                        case EventType.Add:
                            AddTailInternal(descriptor.MetadataId);
                            break;
                        default: break;
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
                    _database.StreamAdd(_redisSettings.StreamKey, new[]
                    {
                        new NameValueEntry(nameof(EventDescriptor.Source), _redisSettings.ConsumerName),
                        new NameValueEntry(nameof(EventDescriptor.EventType), EventType.Add.ToString()),
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
            var retVal = _internal.AddTail(id);

            if (GetInstanceHeadIdInternal() == null)
            {
                SetInstanceHeadIdInternal(_internal.HeadId);
            }

            return retVal;
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
            _distributedLock = DistributedLock.Take(_database, _redisSettings.LockKey, _redisSettings.IdSeedLockTtl);

            while (IsSynchronizationRequired()) 
            {
                // local tail is lagging.
                // Allow synchronizer to run and update the the local tail.
                // The id generator can then generate an id that is after the global tail.
                Thread.Sleep(150);
            }

            return _internal.GenerateNextId(out nextId);
        }

        private bool IsSynchronizationRequired()
        {
            var globalTailId = GetGlobalTailIdInternal();
            // Synchronization not need if global tail is null
            if (globalTailId == null) return false;
            // Synchronization is not needed if global tail is less than
            // the tailId (from previous session) at the time of metadata initialization
            else if (globalTailId <= synchronizeAfter) return false;
            // Synchronization is needed the global tail id greater than synchronizeAfter
            // and tail id is null
            else if (TailId == null) return true;
            // Synchronization is not required  if the local tail is at global tail
            else if (globalTailId == TailId) return false;
            // The code should never come to this line
            // but have to put it for syntactical correctness
            else return true;
        }

        public bool Remove(Guid id)
        {
            _locker.EnterWriteLock();
            try
            {
                var retVal = _internal.Remove(id);
                SetInstanceHeadIdInternal(_internal.HeadId);
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
            var tailRaw = _database.StringGet(_redisSettings.GlobalTailIdKey);

            if (tailRaw.HasValue && Guid.TryParse(tailRaw.ToString(), out var parsed)) globalTailId = parsed;
            return globalTailId;
        }

        private bool SetGlobalTailIdInternal(Guid newId)
        {
            return _database.StringSet(_redisSettings.GlobalTailIdKey, newId.ToString("N"));
        }

        private Guid? GetInstanceHeadIdInternal()
        {
            Guid? instanceHeadId = null;
            var headRaw = _database.StringGet(_redisSettings.InstanceHeadIdKey);

            if (headRaw.HasValue && Guid.TryParse(headRaw.ToString(), out var parsed)) instanceHeadId = parsed;
            return instanceHeadId;
        }

        private bool SetInstanceHeadIdInternal(Guid? newId)
        {
            return _database.StringSet(_redisSettings.InstanceHeadIdKey, newId?.ToString("N"));
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _syncerCTS.Cancel();
                    synchronizer.Join();
                    _syncerCTS = null;
                    synchronizer = null;
                    _internal.Dispose();
                    _internal = null;
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

    public record RedisSettings
    {
        public string AppName { get; init; }
        public string MetadataKey => $"{AppName}:metadata";
        public string DataKey => $"{AppName}:data";
        public string StreamKey => $"{MetadataKey}:stream";
        //public string ConsumerGroup { get; init; } = "default";
        public string ConsumerGroupKey => $"{AppName}:consumerGroups:{ConsumerName}";
        public string ConsumerNameSuffix { get; init; } = "default";
        public string ConsumerName => $"{Environment.MachineName}:{ConsumerNameSuffix}";
        public string LockKey => $"{MetadataKey}:lock";
        public string GlobalTailIdKey => $"{MetadataKey}:tailId";
        public string InstanceHeadIdKey { get => $"{MetadataKey}:headId:{ConsumerName}"; }
        public bool ResumeSession { get; init; } = false;
        public int IdSeedLockTtlMs { get; init; } = 5000; // 5 second default
        public TimeSpan IdSeedLockTtl => TimeSpan.FromMilliseconds(IdSeedLockTtlMs);
    }

    public record EventDescriptor(string Source, string Destination, EventType EventType, Guid EventId, Guid MetadataId);

    public static class StreamEntryExtensions
    {
        public static IEnumerable<EventDescriptor> Parse(this IEnumerable<StreamEntry> entries, string skipSource, string acceptDestination)
        {
            foreach (var entry in entries)
            {
                if (entry.TryParse(skipSource, acceptDestination, out var descriptor)) yield return descriptor;
            }
        }
        public static bool TryParse(this StreamEntry entry, 
                                    string skipSource, 
                                    string acceptDestination, 
                                    out EventDescriptor? descriptor)
        {
            var source = string.Empty;
            var destination = "*";
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
                    case nameof(EventDescriptor.Destination):
                        destination = value.Value;
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
            if (destination == "*" || destination == acceptDestination)
            {
                if (source == skipSource)
                {
                    descriptor = null;
                    return false;
                }
            }
            descriptor = new EventDescriptor(source, destination, eventType, eventId, metadataId);
            return true;
        }
        public static IEnumerable<Guid> GetSetKeys(this IDatabase database, 
                                                   RedisSettings redisSettings, 
                                                   int pageSize = 1000)
        {
            var cursor = "0";
            do
            {
                var res = (RedisResult[])database.Execute("SCAN", cursor, "TYPE", "set", "COUNT", pageSize);

                cursor = (string)res[0];

                foreach (var k in (RedisResult[])res[1])
                {
                    var strKey = k.ToString();
                    if (strKey.StartsWith(redisSettings.DataKey))
                    {
                        // skipping datakey length + 1 because there is a : after datakey
                        yield return Guid.Parse(strKey.Substring(redisSettings.DataKey.Length + 1));
                    }
                }

            } while (cursor != "0");
        }
    }

    public enum EventType
    {
        None,
        Add
    }
}
