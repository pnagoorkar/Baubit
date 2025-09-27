using FluentResults;

namespace Baubit.Caching
{
    public interface IMetadata
    {
        long Count { get; }
        long? HeadId { get; }
        long? TailId { get; }

        bool AddTail(long id);
        bool Clear();
        bool ContainsKey(long id);
        bool GetNextId(long? id, out long? nextId);
        bool GetIdsThrough(long id, out IEnumerable<long> ids);
        bool Remove(long id);
    }
}
