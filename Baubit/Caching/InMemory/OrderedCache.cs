using Microsoft.Extensions.Logging;

namespace Baubit.Caching.InMemory
{
    public sealed class OrderedCache<TValue> : AOrderedCache<TValue>
    {
        public OrderedCache(Configuration cacheConfiguration, 
                            IDataStore<TValue>? l1DataStore, 
                            IDataStore<TValue> l2DataStore, 
                            IMetadata metadata, 
                            ILoggerFactory loggerFactory) : base(cacheConfiguration, l1DataStore, l2DataStore, metadata, loggerFactory)
        {
        }
    }
}
