using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.Identity;
using Microsoft.Extensions.Logging;

namespace Baubit.Test.Caching.Setup
{
    public class DummyStore<TValue> : AStore<TValue>
    {
        public override Guid? HeadId => null;

        public override Guid? TailId => null;

        public DummyStore(long? minCap, long? maxCap, ILoggerFactory loggerFactory) : base(minCap, maxCap, loggerFactory)
        {
        }

        public DummyStore(ILoggerFactory loggerFactory) : this(null, null, loggerFactory)
        {

        }

        public override bool Add(IEntry<TValue> entry) => true;

        public override bool Add(Guid id, TValue value, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(id, value);
            return Add(entry);
        }

        public override bool Clear() => true;

        public override bool GetCount(out long count)
        {
            count = 0;
            return true;
        }

        public override bool GetEntryOrDefault(Guid? id, out IEntry<TValue>? entry)
        {
            entry = null;
            return true;
        }

        public override bool GetValueOrDefault(Guid? id, out TValue? value)
        {
            value = default;
            return true;
        }

        public override bool Remove(Guid id, out IEntry<TValue>? entry)
        {
            entry = new Entry<TValue>(id, default);
            return true;
        }

        public override bool Update(IEntry<TValue> entry) => true;

        public override bool Update(Guid id, TValue value) => true;

        protected override void DisposeInternal()
        {

        }
    }
}
