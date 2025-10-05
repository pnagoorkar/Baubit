using Baubit.Serialization.MessagePack;
using Microsoft.Extensions.Logging;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;

namespace Baubit.Caching.Redis
{
    public class Store<TValue> : AStore<TValue>
    {
        private IServer _server;
        private IDatabase _database;
        private ISerializer _serializer;
        private RedisSettings _redisSettings;

        public Store(long? minCap,
                     long? maxCap,
                     IDatabase database,
                     IServer server,
                     ISerializer serializer,
                     RedisSettings redisSettings,
                     ILoggerFactory loggerFactory) : base(minCap, maxCap, loggerFactory)
        {
            _server = server;
            _database = database;
            _serializer = serializer;
            _redisSettings = redisSettings;
        }

        // No inherent ordering is tracked with per-id Sets.
        public override Guid? HeadId => null;
        public override Guid? TailId => null;

        public override bool Add(IEntry<TValue> entry)
        {
            if (!_serializer.TrySerialize((Entry<TValue>)entry, out var bytes)) return false;
            if (!_database.SetAdd(GetPrefixedKey(entry.Id.ToString()), bytes)) return false;
            return true;
        }

        public override bool Add(Guid id, TValue value, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(id, value);
            return Add(entry);
        }

        public override bool GetCount(out long count)
        {
            // Counts all keys in this Redis DB. If the DB is shared, this reflects total keys, not just this store.
            count = (long)_server.DatabaseSize(_database.Database);
            return true;
        }

        public override bool GetEntryOrDefault(Guid? id, out IEntry<TValue>? entry)
        {
            entry = default;
            if (id == null) return true; // "OrDefault": treat null/missing as not-found, but not an error.

            var key = GetPrefixedKey(id.Value.ToString());

            try
            {
                if (!_database.KeyExists(key)) return true;

                var members = _database.SetMembers(key);
                if (members == null || members.Length == 0) return true;

                var raw = (byte[])members[0];
                if (_serializer.TryDeserialize<Entry<TValue>>(raw, out var deserialized))
                {
                    entry = deserialized;
                }
                return true;
            }
            catch
            {
                entry = default;
                return false;
            }
        }

        public override bool GetValueOrDefault(Guid? id, out TValue? value)
        {
            value = default;
            var ok = GetEntryOrDefault(id, out var entry);

            if (!ok) return false;
            if (entry != null)
            {
                value = entry.Value;
            }
            return true;
        }

        public override bool Remove(Guid id, out IEntry<TValue>? entry)
        {
            // Nodes will not delete anything.
            // They will only adjust (soft delete) their headIds
            return GetEntryOrDefault(id, out entry);
        }

        public override bool Update(IEntry<TValue> entry)
        {
            var key = entry.Id.ToString();

            if (!_serializer.TrySerialize((Entry<TValue>)entry, out var bytes))
                return false;

            try
            {
                // Ensure replacement (avoid multiple members in the Set)
                _database.KeyDelete(key);
                return _database.SetAdd(GetPrefixedKey(key), bytes);
            }
            catch
            {
                return false;
            }
        }

        public override bool Update(Guid id, TValue value)
        {
            var e = new Entry<TValue>(id, value);
            return Update(e);
        }

        private string GetPrefixedKey(string suffix)
        {
            return $"{_redisSettings.DataKey}:{suffix}";
        }

        protected override void DisposeInternal()
        {
            // We don't own the underlying connection, DB, or server; nothing to dispose here.
            // Null out references to help GC and guard against accidental use after dispose.
            _server = null!;
            _database = null!;
            _serializer = null!;
        }
    }
}
