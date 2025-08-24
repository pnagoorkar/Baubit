using FluentResults;

namespace Baubit.Caching
{
    public class Metadata : IMetadata
    {
        //public LinkedList<long> CurrentOrder { get; init; } = new LinkedList<long>();
        //public Dictionary<long, LinkedListNode<long>> IdNodeMap { get; init; } = new Dictionary<long, LinkedListNode<long>>();

        public Dictionary<long, IdMap> IdNodeMap { get; init; } = new Dictionary<long, IdMap>();
        public long Count { get => IdNodeMap.Count; }

        public IdMap CurrentHead { get; private set; }
        public IdMap CurrentTail { get; private set; }


        public long? HeadId { get => CurrentHead?.CurrentId; }
        public long? TailId { get => CurrentTail?.CurrentId; }


        //public long? HeadId { get => CurrentOrder?.First?.Value; }
        //public long? TailId { get => CurrentOrder?.First?.Value; }

        public Result Clear()
        {
            return Result.Try(() =>
            {
                IdNodeMap.Clear();
                CurrentHead = null;
                CurrentTail = null;
            });
        }

        //public Result Clear()
        //{
        //    return Result.Try(() => CurrentOrder.Clear()).Bind(() => Result.Try(() => IdNodeMap.Clear()));
        //}

        public Result AddTail(long id)
        {
            return Result.Try(() =>
            {
                if (CurrentHead == null)
                {
                    CurrentHead = CurrentTail = new IdMap { CurrentId = id };
                }
                else
                {
                    var previousTail = CurrentTail;
                    CurrentTail = new IdMap { CurrentId = id, PreviousId = previousTail.CurrentId };
                    previousTail.NextId = id;
                }
                IdNodeMap.Add(id, CurrentTail);
            });
        }

        //public Result AddTail(long id)
        //{
        //    return Result.Try(() => CurrentOrder.AddLast(id))
        //                 .Bind(node => Result.Try(() => IdNodeMap.Add(id, node)));
        //}

        public Result Remove(long id)
        {
            return Result.Try(() =>
            {
                if (IdNodeMap.Remove(id, out var map))
                {
                    if (id == HeadId)
                    {
                        IdNodeMap[CurrentHead.NextId.Value].PreviousId = null;
                        CurrentHead = CurrentHead.NextId.HasValue ? IdNodeMap[CurrentHead.NextId.Value] : null;
                    }
                    else if (id == TailId)
                    {
                        IdNodeMap[CurrentTail.PreviousId.Value].NextId = null;
                        CurrentTail = CurrentTail.PreviousId.HasValue ? IdNodeMap[CurrentTail.PreviousId.Value] : null;
                    }
                    else
                    {
                        IdNodeMap[IdNodeMap[id].PreviousId.Value].NextId = IdNodeMap[IdNodeMap[id].NextId.Value].CurrentId;
                        IdNodeMap[IdNodeMap[id].NextId.Value].PreviousId = IdNodeMap[IdNodeMap[id].PreviousId.Value].CurrentId;
                    }
                }
            });
        }

        //public Result Remove(long id)
        //{
        //    return Result.OkIf(IdNodeMap.Remove(id, out _), "<TBD>")
        //                 .Bind(() => Result.OkIf(CurrentOrder.Remove(id), "<TBD>"))
        //                 .Bind(() => Result.Ok());
        //}

        public bool ContainsKey(long id)
        {
            return IdNodeMap.ContainsKey(id);
        }

        public Result<long?> GetNextId(long? id)
        {
            return id == null || IsIdSmallerThanHeadId(id) ? HeadId : ContainsKey(id.Value) && IdNodeMap[id.Value].NextId != null ? Result.Ok<long?>(IdNodeMap[id.Value].NextId.Value) : Result.Ok(default(long?));
        }
        //public Result<long?> GetNextId(long? id)
        //{
        //    return id == null || IsIdSmallerThanHeadId(id) ? HeadId : ContainsKey(id.Value) && IdNodeMap[id.Value].Next != null ? Result.Ok<long?>(IdNodeMap[id.Value].Next.Value) : Result.Ok(default(long?));
        //    //return id == null || IsIdSmallerThanHeadId(id) ? HeadId : IdNodeMap.TryGetValue(id.Value, out var node) && node.Next != null ? Result.Ok<long?>(node.Next.Value) : Result.Ok(default(long?));
        //}
        private bool IsIdSmallerThanHeadId(long? id) => id.HasValue && HeadId.HasValue && id < HeadId;
    }

    public class IdMap
    {
        public long CurrentId { get; set; }
        public long? PreviousId { get; set; }
        public long? NextId { get; set; }
    }
}
