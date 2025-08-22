using Baubit.Observation;
using FluentResults;

namespace Baubit.Test.Aggregation.Aggregator.Setup
{
    public class EventConsumer : ISubscriber<TestEvent>
    {
        public int Id { get; private init; }

        private static int idSeed = 1;
        private IDisposable? subscription;

        public Task Completion { get; private init; }

        private TaskCompletionSource _taskCompletionSource;
        public EventConsumer(IPublisher<TestEvent> publisher)
        {
            Id = idSeed++;
            subscription = publisher?.Subscribe(this).Value;
            _taskCompletionSource = new TaskCompletionSource();
            Completion = _taskCompletionSource.Task;
        }

        public Result OnCompleted()
        {
            return Result.Try(() => _taskCompletionSource.SetResult());
        }

        public Result OnError(Exception error)
        {
            return Result.Try(() => _taskCompletionSource.SetException(error));
        }

        public virtual Result OnNext(TestEvent value)
        {
            return Result.Try(() => value.Trace.Add(new Receipt(Id, DateTime.Now)));
        }

        public virtual void Dispose()
        {
            subscription?.Dispose();
            subscription = null;
        }

        public async Task OnNext(TestEvent value, CancellationToken cancellationToken)
        {
            value.Trace.Add(new Receipt(Id, DateTime.Now));
        }
    }
}
