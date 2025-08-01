using Baubit.Observation;
using Baubit.States;
using FluentResults;

namespace Baubit.Test.States.State.Setup
{
    public class MyStateObserver : ISubscriber<StateChanged<MyStatefulType.States>>
    {
        public Queue<StateChanged<MyStatefulType.States>> ChangeEvents { get; init; } = new Queue<StateChanged<MyStatefulType.States>>();
        public Result OnCompleted()
        {
            throw new NotImplementedException();
        }

        public Result OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public Result OnNext(StateChanged<MyStatefulType.States> value)
        {
            ChangeEvents.Enqueue(value);
            return Result.Ok();
        }
    }
}
