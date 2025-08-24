using FluentResults;

namespace Baubit.Caching
{
    public interface IMetadata
    {
        long Count { get; }
        //LinkedList<long> CurrentOrder { get; init; }
        long? HeadId { get; }
        //Dictionary<long, LinkedListNode<long>> IdNodeMap { get; init; }
        Dictionary<long, IdMap> IdNodeMap { get; init; }
        long? TailId { get; }

        Result AddTail(long id);
        Result Clear();
        bool ContainsKey(long id);
        Result<long?> GetNextId(long? id);
        Result Remove(long id);
    }
}
