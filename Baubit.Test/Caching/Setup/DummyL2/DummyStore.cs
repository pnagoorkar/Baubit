using Baubit.Caching;
using Baubit.Caching.InMemory;
using FluentResults;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Baubit.Test.Caching.Setup.DummyL2
{
    public class DummyStore<TValue> : ADataStore<TValue>
    {
        public override long? HeadId => null;

        public override long? TailId => null;

        public override Result Add(IEntry<TValue> entry) => Result.Ok();

        private long idSeed = 0;

        public DummyStore(long? minCap, long? maxCap, ILoggerFactory loggerFactory) : base(minCap, maxCap, loggerFactory)
        {
        }

        public DummyStore(ILoggerFactory loggerFactory) : this(null, null, loggerFactory)
        {
            
        }

        public override Result<IEntry<TValue>> Add(TValue value) => Result.Ok<IEntry<TValue>>(new Entry<TValue>(++idSeed, value));

        public override Result Clear() => Result.Ok();

        public override Result<long> GetCount() => Result.Ok((long)0);

        public override Result<IEntry<TValue>?> GetEntryOrDefault(long? id) => Result.Ok(default(IEntry<TValue>));

        public override Result<TValue?> GetValueOrDefault(long? id) => Result.Ok(default(TValue));

        public override Result<IEntry<TValue>?> Remove(long id) => Result.Ok<IEntry<TValue>>(new Entry<TValue>(id, default));

        public override Result<IEntry<TValue>> Update(IEntry<TValue> entry) => Result.Ok(entry);

        public override Result<IEntry<TValue>> Update(long id, TValue value) => Result.Ok<IEntry<TValue>>(new Entry<TValue>(id, value));

        protected override void DisposeInternal()
        {

        }
    }
}
