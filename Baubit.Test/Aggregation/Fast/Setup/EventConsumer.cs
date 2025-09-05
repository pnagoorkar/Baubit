using Baubit.Observation;
using FluentResults;

namespace Baubit.Test.Aggregation.Fast.Setup
{
    public class EventConsumer : ISubscriber<TestEvent>
    {
        public int Id { get; private init; }

        private static int idSeed = 1;

        public Task Completion { get; private init; }

        private TaskCompletionSource _taskCompletionSource;
        CancellationTokenSource subsciptionCTS = new CancellationTokenSource();
        public EventConsumer(IPublisher<TestEvent> publisher)
        {
            Id = idSeed++;
            publisher.SubscribeAsync(this);
            _taskCompletionSource = new TaskCompletionSource();
            Completion = _taskCompletionSource.Task;
        }

        public bool OnCompleted()
        {
            _taskCompletionSource.SetResult();
            return true;
        }

        public bool OnError(Exception error)
        {
            _taskCompletionSource.SetException(error);
            return true;
        }

        public virtual bool OnNext(TestEvent value)
        {
            value.Trace.Add(new Receipt(Id, DateTime.Now));
            return true;
        }

        public virtual void Dispose()
        {
            subsciptionCTS.Cancel();
        }

        public async Task OnNext(TestEvent value, CancellationToken cancellationToken)
        {
            value.Trace.Add(new Receipt(Id, DateTime.Now));
        }
    }
}
