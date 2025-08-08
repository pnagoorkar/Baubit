using Baubit.Caching;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.Caching.Setup
{
    public class DummyCache<TValue> : AOrderedCache<TValue>
    {
        private long idSeed = 0;

        public DummyCache(Baubit.Caching.Configuration cacheConfiguration,
                          ILoggerFactory loggerFactory) : base(cacheConfiguration, loggerFactory)
        {
        }

        protected override Result DeleteAll() => Result.Ok();

        protected override Result DeleteAllMetadata() => Result.Ok();

        protected override Result DeleteMetadata(long id) => Result.Ok();

        protected override Result<IEntry<TValue>> DeleteStorage(long id) => Result.Ok<IEntry<TValue>>(new Entry(id, default(TValue)));

        protected override void DisposeInternal()
        {

        }

        protected override Result<IEntry<TValue>> Fetch(long id)
        {
            throw new NotImplementedException();
        }

        protected override Result<IEntry<TValue>> FetchNext(long id) => Result.Ok<IEntry<TValue>>(null);

        protected override Result<long> GetCurrentCount() => 0;

        protected override Result<Metadata> GetCurrentHead()
        {
            throw new NotImplementedException();
        }

        protected override Result<Metadata> GetCurrentTail() => Result.Ok(new Metadata { Id = idSeed, Previous = idSeed - 1 });

        protected override Result<Metadata> GetMetadata(long id) => Result.Ok(new Metadata { Id = id });

        protected override Result<IEntry<TValue>> Insert(TValue value) => Result.Ok<IEntry<TValue>>(new Entry(++idSeed, value));

        protected override Result<IEntry<TValue>> UpdateInternal(long id, TValue value) => Result.Ok<IEntry<TValue>>(new Entry(id, value));

        protected override Result Upsert(IEnumerable<Metadata> metadata) => Result.Ok();

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
