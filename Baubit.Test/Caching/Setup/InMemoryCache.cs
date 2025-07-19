using Baubit.Caching;
using FluentResults;

namespace Baubit.Test.Caching.Setup
{
    public class InMemoryCache<TValue> : AOrderedCache<TValue>
    {
        private long _seq;
        private readonly SortedDictionary<long, Entry<TValue>> _data = new();
        private readonly Dictionary<long, Metadata> metadataDictionary = new();

        protected override Result<IEntry<TValue>> Insert(TValue value)
        {
            return Result.Try(() => Interlocked.Increment(ref _seq))
                         .Bind(id => Result.Try(() => new Entry<TValue>(id, value)))
                         .Bind(entry => Result.Try(() => _data.Add(entry.Id, entry))
                                              .Bind(() => Result.Ok<IEntry<TValue>>(entry)));
        }

        protected override Result<IEntry<TValue>> Fetch(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), "Not found")
                         .Bind(() => Result.Try(() => _data[id]))
                         .Bind(entry => Result.Ok<IEntry<TValue>>(entry));
        }

        protected override Result<IEntry<TValue>> DeleteStorage(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), "Not found")
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
                        metadataDictionary.Add(m.Id, m);
                    }
                }
            });
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
            return Result.Try(() => metadataDictionary.Remove(id)).Bind(_ => Result.Ok());
        }

        protected override Result DeleteAllMetadata()
        {
            return Result.Try(() => metadataDictionary.Clear());
        }
    }
}
