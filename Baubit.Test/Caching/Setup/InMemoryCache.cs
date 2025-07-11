using Baubit.Caching;
using FluentResults;

namespace Baubit.Test.Caching.Setup
{
    public class InMemoryCache<TValue> : APersistentCache<TValue>
    {
        private long _seq;
        private readonly SortedDictionary<long, Entry<TValue>> _data = new();

        protected override Task<Result<IEntry<TValue>>> InsertAsync(TValue value)
        {
            return Result.Try(() => Interlocked.Increment(ref _seq))
                         .Bind(id => Result.Try(() => new Entry<TValue>(id, value)))
                         .Bind(entry => Result.Try(() => _data.Add(entry.Id, entry))
                                              .Bind(() => Task.FromResult(Result.Ok<IEntry<TValue>>(entry))));
        }

        protected override Task<Result<IEntry<TValue>>> FetchAsync(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), "Not found")
                         .Bind(() => Result.Try(() => _data[id]))
                         .Bind(entry => Task.FromResult(Result.Ok<IEntry<TValue>>(entry)));
        }

        protected override Task<Result<IEntry<TValue>>> DeleteStorageAsync(long id)
        {
            return Result.OkIf(_data.ContainsKey(id), "Not found")
                         .Bind(() => Result.OkIf(_data.Remove(id, out var entry), "Remove failed")
                                           .Bind(() => Task.FromResult(Result.Ok<IEntry<TValue>>(entry))));
        }

        protected override void DisposeInternal()
        {
            _data.Clear();
        }
    }
}
