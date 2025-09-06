using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    /// <summary>
    /// Helper extensions for <see cref="IOrderedCache{TValue}"/> to simplify value access and enumeration
    /// in both synchronous and asynchronous scenarios.
    /// </summary>
    public static class CachingExtensions
    {
        /// <summary>
        /// Reads the value for the provided <paramref name="id"/> if present.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="cache">The ordered cache.</param>
        /// <param name="id">The entry identifier.</param>
        /// <param name="value">On success, receives the value; otherwise default.</param>
        /// <returns><c>true</c> if the entry exists; otherwise <c>false</c>.</returns>
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

        /// <summary>
        /// Enumerates values starting from <paramref name="startingId"/> (inclusive) when provided,
        /// otherwise from the head. Sequence advances by repeatedly fetching the next entry.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="cache">The ordered cache.</param>
        /// <param name="startingId">An optional starting id to include as the first item.</param>
        /// <returns>An enumerable sequence of values in ascending id order.</returns>
        public static IEnumerable<TValue> EnumerateValues<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            return cache.EnumerateEntries(startingId).Select(entry => entry.Value);
        }

        /// <summary>
        /// Enumerates entries starting from <paramref name="startingId"/> (inclusive) when provided,
        /// otherwise from the head. Sequence advances via <see cref="IOrderedCache{TValue}.GetNextOrDefault(long?, out IEntry{TValue}?)"/>.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="cache">The ordered cache.</param>
        /// <param name="startingId">An optional starting id to include as the first item.</param>
        /// <returns>An enumerable sequence of entries in ascending id order.</returns>
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

        /// <summary>
        /// Asynchronously enumerates values in ascending id order. When the sequence reaches the tail,
        /// the method awaits the next appended entry (via <see cref="IOrderedCache{TValue}.GetNextAsync(long?, CancellationToken)"/>)
        /// until <paramref name="cancellationToken"/> is signaled.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="cache">The ordered cache.</param>
        /// <param name="startingId">Optional id to include first; defaults to the head.</param>
        /// <param name="cancellationToken">A token used to cancel enumeration.</param>
        /// <returns>An async sequence of values.</returns>
        public static async IAsyncEnumerable<TValue> EnumerateValuesAsync<TValue>(this IOrderedCache<TValue> cache, long?
                                                                                  startingId = null,
                                                                                  [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entry in cache.EnumerateEntriesAsync(startingId, cancellationToken).ConfigureAwait(false))
            {
                yield return entry.Value;
            }
        }

        /// <summary>
        /// Asynchronously enumerates entries in ascending id order, awaiting new entries when at the tail
        /// until <paramref name="cancellationToken"/> is signaled.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="cache">The ordered cache.</param>
        /// <param name="startingId">Optional id to include first; defaults to the head.</param>
        /// <param name="cancellationToken">A token used to cancel enumeration.</param>
        /// <returns>An async sequence of entries.</returns>
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
                    nextEntry = await cache.GetNextAsync(id, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException tExp) { }
                if (!cancellationToken.IsCancellationRequested && nextEntry != null)
                {
                    yield return nextEntry;
                    id = nextEntry.Id;
                }
                else break;

            } while (!cancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// Applies a synchronous accumulator to each element of an async sequence, short‑circuiting
        /// when <paramref name="func"/> returns <c>false</c> or when <paramref name="cancellationToken"/> is signaled.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="asyncEnumerable">The async sequence.</param>
        /// <param name="func">A function invoked per item; return <c>false</c> to stop with failure.</param>
        /// <param name="cancellationToken">A token to cancel iteration.</param>
        /// <returns><c>true</c> if the aggregation ran to completion; otherwise <c>false</c>.</returns>
        /// <exception cref="Exception">Thrown when <paramref name="func"/> throws; message is <c>"ka-boom!"</c>.</exception>
        public static async Task<bool> AggregateAsync<T>(this IAsyncEnumerable<T> asyncEnumerable,
                                                           Func<T, bool> func,
                                                           CancellationToken cancellationToken = default)
        {
            await foreach (var item in asyncEnumerable.ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (!func(item)) throw new Exception("ka-boom!");
            }
            return true;
        }

        /// <summary>
        /// Applies an asynchronous accumulator to each element of an async sequence, short‑circuiting
        /// when <paramref name="func"/> returns <c>false</c> or when <paramref name="cancellationToken"/> is signaled.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="asyncEnumerable">The async sequence.</param>
        /// <param name="func">An async function invoked per item; return <c>false</c> to stop with failure.</param>
        /// <param name="cancellationToken">A token to cancel iteration.</param>
        /// <returns><c>true</c> if the aggregation ran to completion; otherwise <c>false</c>.</returns>
        /// <exception cref="Exception">Thrown when <paramref name="func"/> throws; message is <c>"ka-boom!"</c>.</exception>
        public static async Task<bool> AggregateAsync<T>(this IAsyncEnumerable<T> asyncEnumerable,
                                                           Func<T, Task<bool>> func,
                                                           CancellationToken cancellationToken = default)
        {
            await foreach (var item in asyncEnumerable.ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (!(await func(item).ConfigureAwait(false))) throw new Exception("ka-boom!");
            }
            return true;
        }
    }
}
