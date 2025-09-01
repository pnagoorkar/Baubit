using Baubit.Caching.InMemory;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching.Fast.InMemory
{
    public class DataStore<TValue> : ADataStore<TValue>
    {
        public override long? HeadId { get => _data.Count > 0 ? _data.Keys.Min() : null; }
        public override long? TailId { get => _data.Count > 0 ? _data.Keys.Max() : null; }

        private long _seq;
        private readonly Dictionary<long, IEntry<TValue>> _data = new();

        private ILogger<DataStore<TValue>> _logger;

        public DataStore(long? minCap,
                         long? maxCap,
                         ILoggerFactory loggerFactory) : base(minCap, maxCap, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataStore<TValue>>();
        }

        public DataStore(ILoggerFactory loggerFactory) : this(null, null, loggerFactory)
        {

        }

        public override bool Add(IEntry<TValue> entry)
        {
            return HasCapacity && _data.TryAdd(entry.Id, entry);
        }

        public override bool Add(TValue value, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(Interlocked.Increment(ref _seq), value);
            return Add(entry);
        }

        public override bool Clear()
        {
            _data.Clear();
            return true;
        }

        public override bool GetCount(out long count)
        {
            count = _data.Count;
            return true;
        }

        public override bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry)
        {
            entry = null;
            return id.HasValue && _data.TryGetValue(id.Value, out entry);
        }

        public override bool GetValueOrDefault(long? id, out TValue? value)
        {
            value = default;
            if (!GetEntryOrDefault(id, out var entry)) return false;
            value = entry.Value;
            return true;
        }

        public override bool Remove(long id, out IEntry<TValue>? entry)
        {
            return _data.Remove(id, out entry);
        }

        public override bool Update(IEntry<TValue> entry)
        {
            if(!_data.ContainsKey(entry.Id)) return false;
            _data[entry.Id] = entry;
            return true;
        }

        public override bool Update(long id, TValue value)
        {
            return Update(new Entry<TValue>(id, value));
        }

        protected override void DisposeInternal()
        {
            _data.Clear();
        }
    }
}
