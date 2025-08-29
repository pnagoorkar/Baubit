using FluentResults;

namespace Baubit.Caching
{
    public class Metadata : IMetadata
    {
        public LinkedList<long> CurrentOrder { get; init; } = new LinkedList<long>();
        public Dictionary<long, LinkedListNode<long>> IdNodeMap { get; init; } = new Dictionary<long, LinkedListNode<long>>();

        public long Count { get => IdNodeMap.Count; }

        public long? HeadId { get => CurrentOrder?.First?.Value; }
        public long? TailId { get => CurrentOrder?.Last?.Value; }

        public Result Clear()
        {
            return Result.Try(() => CurrentOrder.Clear()).Bind(() => Result.Try(() => IdNodeMap.Clear()));
        }

        public Result AddTail(long id)
        {
            return Result.Try(() => CurrentOrder.AddLast(id))
                         .Bind(node => Result.Try(() => IdNodeMap.Add(id, node)));
        }

        public Result Remove(long id)
        {
            return Result.OkIf(IdNodeMap.Remove(id, out _), "<TBD>")
                         .Bind(() => Result.OkIf(CurrentOrder.Remove(id), "<TBD>"))
                         .Bind(() => Result.Ok());
        }

        public bool ContainsKey(long id)
        {
            return IdNodeMap.ContainsKey(id);
        }

        public Result<long?> GetNextId(long? id)
        {
            return Result.Try(() => 
            {
                if (id == null) return HeadId;
                else if (IsIdSmallerThanHeadId(id)) return HeadId;
                else if (IsIdTailId(id)) return null;
                else if (id.HasValue && IdNodeMap.TryGetValue(id.Value, out var node)) return node.Next.Value;
                else throw new Exception("Midsequence id missing!"); // If an id is neither null, nor less than head nor tail nor an in-between id and the id is not found in IdNodeMap means the value was deleted out of order. For an OrderedCache, this is unexpected
            });
        }

        private bool IsIdSmallerThanHeadId(long? id) => id.HasValue && HeadId.HasValue && id < HeadId;

        private bool IsIdTailId(long? id) => id.HasValue && TailId.HasValue && id == TailId;
    }
    //public class Metadata : IMetadata
    //{
    //    public Dictionary<long, IdMap> IdNodeMap { get; init; } = new Dictionary<long, IdMap>();
    //    public long Count { get => IdNodeMap.Count; }

    //    public IdMap CurrentHead { get; private set; }
    //    public IdMap CurrentTail { get; private set; }


    //    public long? HeadId { get => CurrentHead?.CurrentId; }
    //    public long? TailId { get => CurrentTail?.CurrentId; }

    //    public Result Clear()
    //    {
    //        return Result.Try(() =>
    //        {
    //            IdNodeMap.Clear();
    //            CurrentHead = null;
    //            CurrentTail = null;
    //        });
    //    }

    //    public Result AddTail(long id)
    //    {
    //        return Result.Try(() =>
    //        {
    //            if (CurrentHead == null)
    //            {
    //                CurrentHead = CurrentTail = new IdMap { CurrentId = id };
    //            }
    //            else
    //            {
    //                var previousTail = CurrentTail;
    //                CurrentTail = new IdMap { CurrentId = id, PreviousId = previousTail.CurrentId };
    //                previousTail.NextId = id;
    //            }
    //            IdNodeMap.Add(id, CurrentTail);
    //        });
    //    }

    //    public Result Remove(long id)
    //    {
    //        return Result.Try(() =>
    //        {
    //            if (IdNodeMap.Remove(id, out var map))
    //            {
    //                if (id == HeadId)
    //                {
    //                    IdNodeMap[CurrentHead.NextId.Value].PreviousId = null;
    //                    CurrentHead = CurrentHead.NextId.HasValue ? IdNodeMap[CurrentHead.NextId.Value] : null;
    //                }
    //                else if (id == TailId)
    //                {
    //                    IdNodeMap[CurrentTail.PreviousId.Value].NextId = null;
    //                    CurrentTail = CurrentTail.PreviousId.HasValue ? IdNodeMap[CurrentTail.PreviousId.Value] : null;
    //                }
    //                else
    //                {
    //                    IdNodeMap[IdNodeMap[id].PreviousId.Value].NextId = IdNodeMap[IdNodeMap[id].NextId.Value].CurrentId;
    //                    IdNodeMap[IdNodeMap[id].NextId.Value].PreviousId = IdNodeMap[IdNodeMap[id].PreviousId.Value].CurrentId;
    //                }
    //            }
    //        });
    //    }

    //    public bool ContainsKey(long id)
    //    {
    //        return IdNodeMap.ContainsKey(id);
    //    }

    //    public Result<long?> GetNextId(long? id)
    //    {
    //        return Result.Try(() =>
    //        {
    //            if (id == null) return HeadId;
    //            else if (IsIdSmallerThanHeadId(id)) return HeadId;
    //            else if (IsIdTailId(id)) return null;
    //            else if (id.HasValue && IdNodeMap.TryGetValue(id.Value, out var map)) return map.NextId;
    //            else throw new Exception("Midsequence id missing!"); // If an id is neither null, nor less than head nor tail nor an in-between id and the id is not found in IdNodeMap means the value was deleted out of order. For an OrderedCache, this is unexpected
    //        });
    //    }

    //    private bool IsIdSmallerThanHeadId(long? id) => id.HasValue && HeadId.HasValue && id < HeadId;

    //    private bool IsIdTailId(long? id) => id.HasValue && TailId.HasValue && id == TailId;
    //}

    //public class IdMap
    //{
    //    public long CurrentId { get; set; }
    //    public long? PreviousId { get; set; }
    //    public long? NextId { get; set; }
    //}
}
