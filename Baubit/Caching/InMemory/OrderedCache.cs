using Baubit.Caching.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baubit.Caching.InMemory
{
    public class OrderedCache<TValue> : AOrderedCache<TValue>
    {
        private long _seq;
        private readonly ConcurrentDictionary<long, Entry> _data = new();
        private readonly ConcurrentDictionary<long, Metadata> metadataDictionary = new();

        private readonly ILogger<OrderedCache<TValue>> _logger;
        public OrderedCache(Configuration cacheConfiguration,
                            ILoggerFactory loggerFactory) : base(cacheConfiguration, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderedCache<TValue>>();
        }

        protected override Result<IEntry<TValue>> Insert(TValue value)
        {
            return Result.Try(() => Interlocked.Increment(ref _seq))
                         .Bind(id => Result.Try(() => new Entry(id, value)))
                         .Bind(entry => Result.Try(() => _data.TryAdd(entry.Id, entry))
                                              .Bind(addRes => Result.OkIf(addRes && _data.ContainsKey(entry.Id), nameof(Insert)).AddReasonIfFailed(new EntryNotFound<TValue>(entry.Id)))
                                              .Bind(() => Result.Ok<IEntry<TValue>>(entry)));
        }

        protected override Result<IEntry<TValue>> Fetch(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), nameof(Fetch)).AddReasonIfFailed(new EntryNotFound<TValue>(id))
                         .Bind(() => Result.Try(() => _data[id]))
                         .Bind(entry => Result.Ok<IEntry<TValue>>(entry));
        }

        protected override Result<IEntry<TValue>?> FetchNext(long id)
        {
            return GetMetadata(id).Bind(metadata => metadata?.Next == null ? Result.Ok(default(IEntry<TValue>))! : Fetch(metadata.Next.Value))!;
        }

        protected override Result<IEntry<TValue>> DeleteStorage(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), nameof(Fetch)).AddReasonIfFailed(new EntryNotFound<TValue>(id))
                         .Bind(() => Result.OkIf(_data.Remove(id, out var entry), "Remove failed")
                                           .Bind(() => Result.Ok<IEntry<TValue>>(entry)));
        }

        protected override Result DeleteAll()
        {
            return Result.Try(() => _data.Clear());
        }

        protected override Result<long> GetCurrentCount()
        {
            return Result.Try(() => (long)_data.Count);
        }

        protected override void DisposeInternal()
        {
            _data.Clear();
            metadataDictionary.Clear();
        }

        protected override Result Upsert(IEnumerable<Metadata> metadata)
        {
            return Result.Try(() =>
            {
                foreach (var m in metadata)
                {
                    if (metadataDictionary.ContainsKey(m.Id))
                    {
                        metadataDictionary[m.Id] = m;
                    }
                    else
                    {
                        metadataDictionary.TryAdd(m.Id, m);
                    }
                }
            });
        }

        protected override Result<IEntry<TValue>> UpdateInternal(long id, TValue value)
        {
            return Result.Try(() => new Entry(id, value))
                         .Bind(entry => Result.Try(() => _data[id] = entry))
                         .Bind(entry => Result.Ok<IEntry<TValue>>(entry));
        }

        protected override Result<Metadata> GetCurrentHead()
        {
            return Result.Ok(metadataDictionary.Values.SingleOrDefault(value => value.Previous == null)!);
        }

        protected override Result<Metadata> GetCurrentTail()
        {
            return Result.Ok(metadataDictionary.Values.SingleOrDefault(value => value.Next == null)!);
        }

        protected override Result<Metadata> GetMetadata(long id)
        {
            return Result.Ok(metadataDictionary.ContainsKey(id) ? metadataDictionary[id] : null)!;
        }

        protected override Result DeleteMetadata(long id)
        {
            return Result.Try(() => metadataDictionary.TryRemove(id, out _)).Bind(removeRes => Result.OkIf(removeRes, "Failed to remove"));
        }

        protected override Result DeleteAllMetadata()
        {
            return Result.Try(() => metadataDictionary.Clear());
        }

        public class Entry : IEntry<TValue>
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
}
