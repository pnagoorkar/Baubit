namespace Baubit.Caching.InMemory
{
    public class Metadata : IMetadata
    {
        public LinkedList<long> CurrentOrder { get; init; } = new LinkedList<long>();
        public Dictionary<long, LinkedListNode<long>> IdNodeMap { get; init; } = new Dictionary<long, LinkedListNode<long>>();

        public long Count { get => IdNodeMap.Count; }

        public long? HeadId { get => CurrentOrder?.First?.Value; }
        public long? TailId { get => CurrentOrder?.Last?.Value; }

        public bool AddTail(long id)
        {
            IdNodeMap.Add(id, CurrentOrder.AddLast(id));
            return true;
        }

        public bool Clear()
        {
            CurrentOrder.Clear();
            IdNodeMap.Clear();
            return true;
        }

        public bool ContainsKey(long id) => IdNodeMap.ContainsKey(id);

        public bool GetNextId(long? id, out long? nextId)
        {
            if (id == null) nextId = HeadId;
            else if (HeadId == null) nextId = null; // if id is not null but HeadId is null means id is the tail that was deleted just before the call arrived here. Return null so the caller can get the next arriving item
            else if (IsIdSmallerThanHeadId(id)) nextId = HeadId;
            else if (IsIdTailId(id)) nextId = null;
            else if (id.HasValue && IdNodeMap.TryGetValue(id.Value, out var node)) nextId = node.Next.Value;
            else nextId = IdNodeMap.Keys.Order().FirstOrDefault(key => key > id); // If an id is neither null, nor less than head nor tail nor an in-between id and the id is not found in IdNodeMap means the value was deleted out of order. Return the next big id after it.
            return true;
        }

        public bool GetIdsThrough(long id, out IEnumerable<long> ids)
        {
            // (Empty store || if id preceeds the head) = do nothing
            if (CurrentOrder.Count == 0 || id < HeadId)
            {
                ids = [];
                return false;
            }

            // If id is at/after the tail -> whole list
            if (id >= TailId)
            {
                ids = Enumerate(CurrentOrder.First!, CurrentOrder.Last!);
                return true;
            }

            if(!IdNodeMap.TryGetValue(id, out var end))
            {
                // this method is intended to be called from the ordered cache and it is assumed that the cache will ALWAYS send an id that IS present in the IdNodeMap.
                // if this method ever gets executed, the above assumption must not longer be true.
                ids = [];
                return false;
            }

            ids = Enumerate(CurrentOrder.First!, end);
            return true;

            static IEnumerable<long> Enumerate(LinkedListNode<long> start, LinkedListNode<long> endInclusive)
            {
                for (var n = start; n != null; n = n.Next)
                {
                    yield return n.Value;
                    if (ReferenceEquals(n, endInclusive)) yield break;
                }
            }
        }

        private bool IsIdSmallerThanHeadId(long? id) => id.HasValue && HeadId.HasValue && id < HeadId;

        private bool IsIdTailId(long? id) => id.HasValue && TailId.HasValue && id == TailId;

        public bool Remove(long id)
        {
            if (IdNodeMap.Remove(id, out var node))
            {
                CurrentOrder.Remove(node);
                return true;
            }
            return false;
        }
    }
}
