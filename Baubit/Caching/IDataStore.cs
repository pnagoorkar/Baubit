using FluentResults;

namespace Baubit.Caching
{
    public interface IDataStore<TValue> : IDisposable
    {
        long CurrentCapacity { get; }
        bool HasCapacity { get; }
        long MaxCapacity { get; init; }
        long MinCapacity { get; init; }
        long TargetCapacity { get; }
        long? HeadId { get; }
        long? TailId { get; }

        Result Add(IEntry<TValue> entry);
        Result<IEntry<TValue>> Add(TValue value);
        Result AddCapacity(int additionalCapacity);
        Result Clear();
        Result CutCapacity(int cap);
        Result<long> GetCount();
        Result<IEntry<TValue>?> GetEntryOrDefault(long? id);
        Result<TValue?> GetValueOrDefault(long? id);
        Result<IEntry<TValue>?> Remove(long id);
        Result<IEntry<TValue>> Update(IEntry<TValue> entry);
        Result<IEntry<TValue>> Update(long id, TValue value);
    }
}
