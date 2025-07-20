using Baubit.IO.Channels;
using FluentResults;
using FluentResults.Extensions;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Baubit.Caching
{
    /// <summary>
    /// Represents an ordered cache for storing and retrieving values by unique identifiers.
    /// Provides methods for adding, retrieving, removing, and enumerating cached entries.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public interface IOrderedCache<TValue> : IDisposable
    {
        /// <summary>
        /// Gets the number of entries currently stored in the cache.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the count of entries, or an error if the operation fails.
        /// </returns>
        Result<long> Count();

        /// <summary>
        /// Adds a value to the cache and returns the created entry.
        /// </summary>
        /// <param name="value">The value to add to the cache.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the created <see cref="IEntry{TValue}"/>, or an error if the operation fails.
        /// </returns>
        Result<IEntry<TValue>> Add(TValue value);

        /// <summary>
        /// Retrieves an entry from the cache by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entry to retrieve.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the <see cref="IEntry{TValue}"/>, or an error if not found.
        /// </returns>
        Result<IEntry<TValue>> Get(long id);

        /// <summary>
        /// Asynchronously retrieves the next entry in the cache after the specified identifier.
        /// </summary>
        /// <param name="id">The identifier to start from, or <c>null</c> to start from the beginning.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that resolves to a <see cref="Result{T}"/> containing the next <see cref="IEntry{TValue}"/>, or an error if not found.
        /// </returns>
        Task<Result<IEntry<TValue>>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an entry from the cache by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entry to remove.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the removed <see cref="IEntry{TValue}"/>, or an error if not found.
        /// </returns>
        Result<IEntry<TValue>> Remove(long id);

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        Result Clear();
    }

    /// <summary>
    /// Extension methods for ordered cache types.
    /// </summary>
    public static class CachingExtensions
    {
        /// <summary>
        /// Asynchronously reads entries from the cache, yielding each result.
        /// </summary>
        /// <typeparam name="TCache">The cache type implementing <see cref="IOrderedCache{TValue}"/>.</typeparam>
        /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
        /// <param name="cache">The cache instance to read from.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of results containing cache entries.</returns>
        public static async IAsyncEnumerable<Result<IEntry<TValue>>> ReadAsync<TCache, TValue>(this TCache cache, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TCache : IOrderedCache<TValue>
        {
            long? id = null;
            do
            {
                yield return await cache.GetNextAsync(id, cancellationToken).Bind(entry => Result.Try(() => { id = entry.Id; return entry; }));

            } while (!cancellationToken.IsCancellationRequested);
        }
    }
}
