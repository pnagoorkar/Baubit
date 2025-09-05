//using FluentResults;

//namespace Baubit.Caching.InMemory
//{
//    public class Metadata : IMetadata
//    {
//        public LinkedList<long> CurrentOrder { get; init; } = new LinkedList<long>();
//        public Dictionary<long, LinkedListNode<long>> IdNodeMap { get; init; } = new Dictionary<long, LinkedListNode<long>>();

//        public long Count { get => IdNodeMap.Count; }

//        public long? HeadId { get => CurrentOrder?.First?.Value; }
//        public long? TailId { get => CurrentOrder?.Last?.Value; }

//        public Result Clear()
//        {
//            return Result.Try(() => CurrentOrder.Clear()).Bind(() => Result.Try(() => IdNodeMap.Clear()));
//        }

//        public Result AddTail(long id)
//        {
//            return Result.Try(() => CurrentOrder.AddLast(id))
//                         .Bind(node => Result.Try(() => IdNodeMap.Add(id, node)));
//        }

//        public Result Remove(long id)
//        {
//            return Result.OkIf(IdNodeMap.Remove(id, out _), "<TBD>")
//                         .Bind(() => Result.OkIf(CurrentOrder.Remove(id), "<TBD>"))
//                         .Bind(() => Result.Ok());
//        }

//        public bool ContainsKey(long id)
//        {
//            return IdNodeMap.ContainsKey(id);
//        }

//        public Result<long?> GetNextId(long? id)
//        {
//            return Result.Try(() => 
//            {
//                if (id == null) return HeadId;
//                else if(HeadId == null) return null; // if id is not null but HeadId is null means id is the tail that was deleted just before the call arrived here. Return null so the caller can get the next arriving item
//                else if (IsIdSmallerThanHeadId(id)) return HeadId;
//                else if (IsIdTailId(id)) return null;
//                else if (id.HasValue && IdNodeMap.TryGetValue(id.Value, out var node)) return node.Next.Value;
//                else throw new Exception("Midsequence id missing!"); // If an id is neither null, nor less than head nor tail nor an in-between id and the id is not found in IdNodeMap means the value was deleted out of order. For an OrderedCache, this is unexpected
//            });
//        }

//        private bool IsIdSmallerThanHeadId(long? id) => id.HasValue && HeadId.HasValue && id < HeadId;

//        private bool IsIdTailId(long? id) => id.HasValue && TailId.HasValue && id == TailId;
//    }
//}
