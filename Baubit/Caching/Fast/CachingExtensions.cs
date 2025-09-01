using System.Runtime.CompilerServices;

namespace Baubit.Caching.Fast
{
    public static class CachingExtensions
    {
        public static bool GetValue<TValue>(this IOrderedCache<TValue> cache, long id, out TValue value)
        {
            value = default;
            if (cache.GetEntryOrDefault(id, out var entry) && entry != null)
            {
                value = entry.Value;
                return true;
            }
            return false;
        }
        public static IEnumerable<TValue> EnumerateValues<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            return cache.EnumerateEntries(startingId).Select(entry => entry.Value);
        }
        public static IEnumerable<IEntry<TValue>> EnumerateEntries<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            if (startingId.HasValue && cache.GetEntryOrDefault(startingId, out var entry) && entry != null)
            {
                yield return entry;
            }

            var id = startingId;
            do
            {
                if (cache.GetNextOrDefault(id, out var nextEntry) && nextEntry != null) // will return head if id is null and continue from there
                {
                    yield return nextEntry;
                    id = nextEntry.Id;
                }
                else break;

            } while (id.HasValue);
        }

        public static async IAsyncEnumerable<TValue> EnumerateValuesAsync<TValue>(this IOrderedCache<TValue> cache, long?
                                                                                  startingId = null,
                                                                                  [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entry in cache.EnumerateEntriesAsync(startingId, cancellationToken).ConfigureAwait(false))
            {
                yield return entry.Value;
            }
        }

        public static async IAsyncEnumerable<IEntry<TValue>> EnumerateEntriesAsync<TValue>(this IOrderedCache<TValue> cache,
                                                                                           long? startingId = null,
                                                                                           [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (startingId.HasValue && cache.GetEntryOrDefault(startingId, out var entry) && entry != null)
            {
                yield return entry;
            }

            var id = startingId;
            do
            {
                var nextEntry = default(IEntry<TValue>);
                try
                {
                    nextEntry = await cache.GetNextAsync(id, cancellationToken);
                }
                catch(TaskCanceledException tExp) { }
                if (!cancellationToken.IsCancellationRequested && nextEntry != null)
                {
                    yield return nextEntry;
                    id = nextEntry.Id;
                }
                else break;

            } while (!cancellationToken.IsCancellationRequested);
        }

        public static async Task<bool> AggregateAsync<T>(this IAsyncEnumerable<T> asyncEnumerable,
                                                           Func<T, bool> func,
                                                           CancellationToken cancellationToken = default)
        {
            await foreach (var item in asyncEnumerable.ConfigureAwait(false))
            {
                if(cancellationToken.IsCancellationRequested) return false;
                if(!func(item)) throw new Exception("ka-boom!");
            }
            return true;
        }
    }
}
