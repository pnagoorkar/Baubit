//using Baubit.IO.Channels;
//using Baubit.Observation;
//using FluentResults;
//using FluentResults.Extensions;
//using System.Collections.Specialized;
//using System.Runtime.CompilerServices;
//using System.Threading.Channels;

//namespace Baubit.Caching
//{
//    /// <summary>
//    /// Defines a contract for an ordered cache that stores and retrieves values by unique identifiers.
//    /// Supports adding, updating, removing, and enumerating entries in a deterministic order.
//    /// </summary>
//    /// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
//    public interface IOrderedCache<TValue> : IDisposable
//    {

//        public long Count { get; }

//        /// <summary>
//        /// Adds a value to the cache.
//        /// </summary>
//        /// <param name="value">The value to insert.</param>
//        /// <returns>
//        /// A <see cref="Result"/> containing the created <see cref="IEntry{TValue}"/>,
//        /// or an error if the operation fails.
//        /// </returns>
//        Result<IEntry<TValue>> Add(TValue value);

//        /// <summary>
//        /// Updates an existing cache entry with a new value.
//        /// </summary>
//        /// <param name="id">The identifier of the entry to update.</param>
//        /// <param name="value">The new value to store.</param>
//        /// <returns>
//        /// A <see cref="Result"/> containing the updated entry,
//        /// or an error if the entry does not exist or the update fails.
//        /// </returns>
//        Result<IEntry<TValue>> Update(long id, TValue value);


//        Result<IEntry<TValue>?> GetEntryOrDefault(long? id);

//        Result<IEntry<TValue>?> GetNextOrDefault(long? id);

//        Result<IEntry<TValue>?> GetFirstOrDefault();

//        Result<IEntry<TValue>?> GetLastOrDefault();

//        /// <summary>
//        /// Asynchronously retrieves the entry following the specified identifier.<br/>
//        /// Returns the first entry (head) if <c>id</c> is less than the id of the current head<br/>
//        /// Awaits and returns the next entry if <c>id</c> is greater than the id of the tail (last entry)
//        /// </summary>
//        /// <param name="id">
//        /// The identifier of the current entry, or <c>null</c> to begin from the head.
//        /// </param>
//        /// <param name="cancellationToken">A cancellation token.</param>
//        /// <returns>
//        /// A task resolving to a <see cref="Result"/> containing the next entry,
//        /// or an error if the operation fails.
//        /// </returns>
//        Task<Result<IEntry<TValue>>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default);

//        /// <summary>
//        /// Removes an entry by its identifier.
//        /// </summary>
//        /// <param name="id">The unique identifier of the entry to remove.</param>
//        /// <returns>
//        /// A <see cref="Result"/> containing the removed entry,
//        /// or an error if the entry does not exist.
//        /// </returns>
//        Result<IEntry<TValue>?> Remove(long id);

//        /// <summary>
//        /// Removes all entries from the cache.
//        /// </summary>
//        /// <returns>
//        /// A <see cref="Result"/> indicating success or failure.
//        /// </returns>
//        Result Clear();
//    }
//}

