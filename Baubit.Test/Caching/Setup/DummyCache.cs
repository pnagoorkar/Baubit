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

        protected override Result ClearL2Store() => Result.Ok();

        protected override Result ClearMetadata() => Result.Ok();

        protected override Result DeleteMetadata(long id) => Result.Ok();

        protected override Result<IEntry<TValue>> DeleteFromL2Store(long id) => Result.Ok<IEntry<TValue>>(new Entry(id, default(TValue)));

        protected override void DisposeL2StoreResources()
        {

        }

        protected override Result<IEntry<TValue>> GetFromL2Store(long id)
        {
            throw new NotImplementedException();
        }

        protected override Result<IEntry<TValue>> GetNextFromL2Store(long id) => Result.Ok<IEntry<TValue>>(null);

        protected override Result<long> GetL2StoreCount() => 0;

        protected override Result<Metadata> GetCurrentHead()
        {
            throw new NotImplementedException();
        }

        protected override Result<Metadata> GetCurrentTail() => Result.Ok(new Metadata { Id = idSeed, Previous = idSeed - 1 });

        protected override Result<Metadata> GetMetadata(long id) => Result.Ok(new Metadata { Id = id });

        protected override Result<IEntry<TValue>> AddToL2Store(TValue value) => Result.Ok<IEntry<TValue>>(new Entry(++idSeed, value));

        protected override Result<IEntry<TValue>> UpdateL2Store(long id, TValue value) => Result.Ok<IEntry<TValue>>(new Entry(id, value));

        protected override Result UpsertL2Store(IEnumerable<Metadata> metadata) => Result.Ok();

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
