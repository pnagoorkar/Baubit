using Baubit.Caching.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Concurrent;

namespace Baubit.Caching.InMemory
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

        public override Result<IEntry<TValue>> Add(TValue value)
        {
            return Result.Try(() => Interlocked.Increment(ref _seq))
                         .Bind(id => Result.Try(() => new Entry<TValue>(id, value)))
                         .Bind(entry => Add(entry).Bind(() => Result.Ok<IEntry<TValue>>(entry)));
        }

        public override Result Add(IEntry<TValue> entry)
        {
            return Result.Try(() => _data.TryAdd(entry.Id, entry))
                         .Bind(addRes => Result.OkIf(addRes, nameof(Add)).AddReasonIfFailed(new FailedToAddEntry<TValue>(entry)));
        }

        public override Result<IEntry<TValue>?> Remove(long id)
        {
            IEntry<TValue> entry = default!;

            return Result.Try(() => _data.Remove(id, out entry!)).Bind(_ => Result.Ok<IEntry<TValue>?>(entry));
        }

        public override Result<IEntry<TValue>> Update(IEntry<TValue> entry)
        {
            return Update(entry.Id, entry.Value);
        }

        public override Result<IEntry<TValue>> Update(long id, TValue value)
        {
            return Result.Try(() => new Entry<TValue>(id, value))
                         .Bind(entry => Result.Try(() => _data[id] = entry))
                         .Bind(entry => Result.Ok<IEntry<TValue>>(entry));
        }

        public override Result<IEntry<TValue>?> GetEntryOrDefault(long? id)
        {
            if (id == null)
            {
                return Result.Ok(default(IEntry<TValue>)).WithReason(IdIsNull.Instance);
            }
            else if (_data.TryGetValue(id.Value, out var val))
            {
                return Result.Ok<IEntry<TValue>?>(val);
            }
            else
            {
                return Result.Ok(default(IEntry<TValue>)).WithReason(new EntryNotFound<TValue>(id.Value));
            }
        }

        public override Result<TValue?> GetValueOrDefault(long? id)
        {
            return GetEntryOrDefault(id).Bind(entry => Result.Ok<TValue?>(entry == null ? default(TValue) : entry.Value));
        }

        public override Result Clear()
        {
            return Result.Try(() => _data.Clear());
        }

        public override Result<long> GetCount()
        {
            return Result.Try(() => (long)_data.Count);
        }

        protected override void DisposeInternal()
        {
            _data.Clear();
        }
    }
    public class Entry<TValue> : IEntry<TValue>
    {
        public long Id { get; init; }
        public DateTime CreatedOnUTC { get; init; } = DateTime.UtcNow;
        public TValue Value { get; init; }
        public Entry(long id, TValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
