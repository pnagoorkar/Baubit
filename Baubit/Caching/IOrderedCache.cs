using FluentResults;

namespace Baubit.Caching
{
    /// <summary>
    /// An ordered, append-only cache with monotonically increasing identifiers.
    /// Supports random access by id, forward iteration, and asynchronous waiting for the next entry.
    /// Implementations are expected to be thread-safe for concurrent readers and writers.
    /// </summary>
    /// <typeparam name="TValue">The type of values held in the cache.</typeparam>
    public interface IOrderedCache<TValue> : IDisposable
    {
        /// <summary>
        /// The number of entries currently present.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Adds a new value to the tail of the cache.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="entry">When the method returns <c>true</c>, contains the created entry and its assigned id.</param>
        /// <returns><c>true</c> if the value was added; otherwise <c>false</c>.</returns>
        bool Add(TValue value, out IEntry<TValue> entry);

        /// <summary>
        /// Updates an existing entry's value identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The entry identifier.</param>
        /// <param name="value">The updated value.</param>
        /// <returns><c>true</c> if the entry was updated; otherwise <c>false</c>.</returns>
        bool Update(long id, TValue value);

        /// <summary>
        /// Gets the entry with the specified identifier if it exists.
        /// </summary>
        /// <param name="id">The identifier to look up.</param>
        /// <param name="entry">On success, the located entry; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry);

        /// <summary>
        /// Gets the next entry after <paramref name="id"/>, or the head entry when <paramref name="id"/> is <c>null</c>.
        /// </summary>
        /// <param name="id">The current id, or <c>null</c> to start from the head.</param>
        /// <param name="entry">On success, the next entry; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetNextOrDefault(long? id, out IEntry<TValue>? entry);

        /// <summary>
        /// Tries to retrieve the first (head) entry.
        /// </summary>
        /// <param name="entry">On success, the first entry; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetFirstOrDefault(out IEntry<TValue>? entry);

        /// <summary>
        /// Returns the id of the first (tail) entry.
        /// </summary>
        /// <param name="id">On success, id of the first entry; otherwise <c>null</c></param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetFirstIdOrDefault(out long? id);

        /// <summary>
        /// Tries to retrieve the last (tail) entry.
        /// </summary>
        /// <param name="entry">On success, the last entry; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetLastOrDefault(out IEntry<TValue>? entry);

        /// <summary>
        /// Returns the id of the last (tail) entry.
        /// </summary>
        /// <param name="id">On success, id of the last entry; otherwise <c>null</c></param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetLastIdOrDefault(out long? id);

        /// <summary>
        /// Asynchronously waits for and returns the next entry after <paramref name="id"/>.
        /// When <paramref name="id"/> is <c>null</c> and the cache is non-empty, the head is returned immediately.
        /// Otherwise, the task completes when a new entry is appended.
        /// </summary>
        /// <param name="id">The id to advance from, or <c>null</c> to start from the head.</param>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        /// <returns>A task that completes with the next entry.</returns>
        Task<IEntry<TValue>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously waits for and returns the first entry (immediately) after the call
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        /// <returns>A task that completes with the first future entry.</returns>
        Task<IEntry<TValue>> GetFutureFirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the entry with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier to remove.</param>
        /// <param name="entry">On success, the removed entry.</param>
        /// <returns><c>true</c> if an entry was removed; otherwise <c>false</c>.</returns>
        bool Remove(long id, out IEntry<TValue>? entry);

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        /// <returns><c>true</c> on success; otherwise <c>false</c>.</returns>
        bool Clear();
    }
}
