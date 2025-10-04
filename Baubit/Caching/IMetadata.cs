using FluentResults;

namespace Baubit.Caching
{
    public interface IMetadata
    {
        long Count { get; }
        Guid? HeadId { get; }
        Guid? TailId { get; }

        internal long ResetRoomCount();
        bool AddTail(Guid id);
        bool ContainsKey(Guid id);
        bool GetNextId(Guid? id, out Guid? nextId);
        bool GenerateNextId(out Guid nextId);
        Task<Guid> GetNextIdAsync(Guid? id, CancellationToken cancellationToken);
        bool GetIdsThrough(Guid id, out IEnumerable<Guid> ids);
        bool Remove(Guid id);
    }
}
