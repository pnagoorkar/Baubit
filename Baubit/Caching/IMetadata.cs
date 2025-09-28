using FluentResults;

namespace Baubit.Caching
{
    public interface IMetadata
    {
        long Count { get; }
        Guid? HeadId { get; }
        Guid? TailId { get; }

        bool AddTail(Guid id);
        bool Clear();
        bool ContainsKey(Guid id);
        bool GetNextId(Guid? id, out Guid? nextId);
        bool GetIdsThrough(Guid id, out IEnumerable<Guid> ids);
        bool Remove(Guid id);
    }
}
