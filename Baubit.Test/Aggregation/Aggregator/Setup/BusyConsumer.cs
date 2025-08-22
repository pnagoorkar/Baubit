using Baubit.Observation;
using FluentResults;

namespace Baubit.Test.Aggregation.Aggregator.Setup
{
    public class BusyConsumer : EventConsumer
    {
        CancellationTokenSource _masterCancellationTokenSource = new CancellationTokenSource();
        public BusyConsumer(IPublisher<TestEvent> publisher) : base(publisher)
        {
        }
        public override Result OnNext(TestEvent value)
        {
            while (!_masterCancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
            return Result.Ok();
        }
        public override void Dispose()
        {
            _masterCancellationTokenSource.Cancel();
            base.Dispose();
        }
    }
}
