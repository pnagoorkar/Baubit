using Baubit.Caching;
using FluentResults;

namespace Baubit.Test.Caching.Setup
{
    public class InMemoryCache<TValue> : APersistentCache<TValue>
    {
        private long _seq;
        private readonly SortedDictionary<long, Entry<TValue>> _data = new();

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
    }
}
