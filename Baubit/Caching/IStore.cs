using FluentResults;

namespace Baubit.Caching
{
    /// <summary>
    /// Abstraction for a storage layer used by <see cref="IOrderedCache{TValue}"/> implementations.
    /// A store may be capacity-bound (e.g., an in‑memory L1) or uncapped (e.g., a backing L2 store).
    /// Implementations are expected to be thread-safe for concurrent readers/writers.
    /// </summary>
    /// <typeparam name="TValue">The value type stored in the cache.</typeparam>
    public interface IStore<TValue> : IDisposable
    {
        /// <summary>
        /// Indicates whether this store has no capacity limit.
        /// When <c>true</c>, capacity-related members are typically ignored.
        /// </summary>
        bool Uncapped { get; }

        /// <summary>
        /// The current usable capacity for additional entries, if meaningful for the implementation.
        /// For bounded stores, this usually represents remaining room (e.g., free slots).
        /// </summary>
        long? CurrentCapacity { get; }

        /// <summary>
        /// Indicates whether the store can accept at least one more entry without resizing.
        /// </summary>
        bool HasCapacity { get; }

        /// <summary>
        /// Maximum capacity the store may grow to, if bounded.
        /// </summary>
        long? MaxCapacity { get; init; }

        /// <summary>
        /// Minimum capacity the store may shrink to, if bounded.
        /// </summary>
        long? MinCapacity { get; init; }

        /// <summary>
        /// The intended/target capacity for the store when using adaptive resizing.
        /// Implementations may converge toward this value over time.
        /// </summary>
        long? TargetCapacity { get; }

        /// <summary>
        /// The identifier of the first (head/oldest) entry present in this store, if available.
        /// </summary>
        Guid? HeadId { get; }

        /// <summary>
        /// The identifier of the last (tail/newest) entry present in this store, if available.
        /// </summary>
        Guid? TailId { get; }

        /// <summary>
        /// Adds an existing entry instance to the store (e.g., promoting from another layer).
        /// </summary>
        /// <param name="entry">The entry instance to add.</param>
        /// <returns><c>true</c> if the entry was accepted; otherwise <c>false</c>.</returns>
        bool Add(IEntry<TValue> entry);

        /// <summary>
        /// Adds a new value to the store, creating a corresponding entry.
        /// </summary>
        /// <param name="id">The id against which to store the value</param>
        /// <param name="value">The value to add.</param>
        /// <param name="entry">When the method returns <c>true</c>, contains the created entry.</param>
        /// <returns><c>true</c> if the value was added; otherwise <c>false</c>.</returns>
        bool Add(Guid id, TValue value, out IEntry<TValue>? entry);

        /// <summary>
        /// Increases the store capacity by the specified amount (implementation-defined semantics).
        /// </summary>
        /// <param name="additionalCapacity">The amount by which to grow capacity.</param>
        /// <returns><c>true</c> if the capacity was adjusted; otherwise <c>false</c>.</returns>
        bool AddCapacity(int additionalCapacity);

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <returns><c>true</c> on success; otherwise <c>false</c>.</returns>
        bool Clear();

        /// <summary>
        /// Decreases the store capacity by the specified amount or to a new target (implementation-defined).
        /// </summary>
        /// <param name="cap">The shrink step or target, depending on implementation.</param>
        /// <returns><c>true</c> if the capacity was adjusted; otherwise <c>false</c>.</returns>
        bool CutCapacity(int cap);

        /// <summary>
        /// Retrieves the total number of entries currently persisted in the store.
        /// </summary>
        /// <param name="count">On success, receives the entry count.</param>
        /// <returns><c>true</c> if the count could be determined; otherwise <c>false</c>.</returns>
        bool GetCount(out long count);

        /// <summary>
        /// Gets an entry by identifier, or a default/null value if not present.
        /// </summary>
        /// <param name="id">The entry identifier.</param>
        /// <param name="entry">On success, the located entry; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetEntryOrDefault(Guid? id, out IEntry<TValue>? entry);

        /// <summary>
        /// Gets a value by identifier, or a default value if not present.
        /// </summary>
        /// <param name="id">The entry identifier.</param>
        /// <param name="value">On success, the located value; otherwise default.</param>
        /// <returns><c>true</c> if the lookup succeeded (even when not found); otherwise <c>false</c>.</returns>
        bool GetValueOrDefault(Guid? id, out TValue? value);

        /// <summary>
        /// Removes an entry by identifier.
        /// </summary>
        /// <param name="id">The entry identifier.</param>
        /// <param name="entry">On success, the removed entry.</param>
        /// <returns><c>true</c> if an entry was removed; otherwise <c>false</c>.</returns>
        bool Remove(Guid id, out IEntry<TValue>? entry);

        /// <summary>
        /// Updates an entry in-place.
        /// </summary>
        /// <param name="entry">The entry to update (identifier and new value).</param>
        /// <returns><c>true</c> if the entry was updated; otherwise <c>false</c>.</returns>
        bool Update(IEntry<TValue> entry);

        /// <summary>
        /// Updates the value for a given identifier.
        /// </summary>
        /// <param name="id">The entry identifier.</param>
        /// <param name="value">The new value.</param>
        /// <returns><c>true</c> if the value was updated; otherwise <c>false</c>.</returns>
        bool Update(Guid id, TValue value);
    }
}
