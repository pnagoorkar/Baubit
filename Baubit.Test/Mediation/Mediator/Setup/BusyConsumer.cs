using Baubit.Observation;
using FluentResults;

namespace Baubit.Test.Mediation.Mediator.Setup
{
    public class BusyConsumer : EventConsumer
    {
        CancellationTokenSource _masterCancellationTokenSource = new CancellationTokenSource();
        public BusyConsumer(IPublisher<object> publisher) : base(publisher)
        {
        }
        public override bool OnNext(TestEvent value)
        {
            while (!_masterCancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
            return true;
        }
        public override void Dispose()
        {
            _masterCancellationTokenSource.Cancel();
            base.Dispose();
        }
    }
}
