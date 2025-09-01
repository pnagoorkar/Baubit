using FluentResults;

namespace Baubit.Caching.Fast
{
    public interface IOrderedCache<TValue> : IDisposable
    {

        public long Count { get; }

        bool Add(TValue value, out IEntry<TValue> entry);

        bool Update(long id, TValue value);

        bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry);

        bool GetNextOrDefault(long? id, out IEntry<TValue>? entry);

        bool GetFirstOrDefault(out IEntry<TValue>? entry);

        bool GetLastOrDefault(out IEntry<TValue>? entry);

        Task<IEntry<TValue>> GetNextAsync(long? id = null, CancellationToken cancellationToken = default);

        bool Remove(long id, out IEntry<TValue>? entry);

        bool Clear();
    }
}
