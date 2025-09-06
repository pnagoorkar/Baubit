using Baubit.Caching;
using Baubit.Caching.InMemory;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.Caching.Setup
{
    public class DummyStore<TValue> : ADataStore<TValue>
    {
        public override long? HeadId => null;

        public override long? TailId => null;

        private long idSeed = 0;

        public DummyStore(long? minCap, long? maxCap, ILoggerFactory loggerFactory) : base(minCap, maxCap, loggerFactory)
        {
        }

        public DummyStore(ILoggerFactory loggerFactory) : this(null, null, loggerFactory)
        {

        }

        public override bool Add(IEntry<TValue> entry) => true;

        public override bool Add(TValue value, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(++idSeed, value);
            return Add(entry);
        }

        public override bool Clear() => true;

        public override bool GetCount(out long count)
        {
            count = 0;
            return true;
        }

        public override bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry)
        {
            entry = null;
            return true;
        }

        public override bool GetValueOrDefault(long? id, out TValue? value)
        {
            value = default;
            return true;
        }

        public override bool Remove(long id, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(id, default);
            return true;
        }

        public override bool Update(IEntry<TValue> entry) => true;

        public override bool Update(long id, TValue value) => true;

        protected override void DisposeInternal()
        {

        }
    }
}
