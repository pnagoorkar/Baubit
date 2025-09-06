using FluentResults;

namespace Baubit.Caching
{
    public interface IStore<TValue> : IDisposable
    {
        bool Uncapped { get; }
        long? CurrentCapacity { get; }
        bool HasCapacity { get; }
        long? MaxCapacity { get; init; }
        long? MinCapacity { get; init; }
        long? TargetCapacity { get; }
        long? HeadId { get; }
        long? TailId { get; }

        bool Add(IEntry<TValue> entry);
        bool Add(TValue value, out IEntry<TValue>? entry);
        bool AddCapacity(int additionalCapacity);
        bool Clear();
        bool CutCapacity(int cap);
        bool GetCount(out long count);
        bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry);
        bool GetValueOrDefault(long? id, out TValue? value);
        bool Remove(long id, out IEntry<TValue>? entry);
        bool Update(IEntry<TValue> entry);
        bool Update(long id, TValue value);
    }
}
