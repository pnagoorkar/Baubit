using Baubit.Test.Traceability.Setup;
using Baubit.Traceability;

namespace Baubit.Test.Traceability
{
    public class Test
    {
        [Fact]
        public void CanObserveTraceables()
        {
            var notificationCount = 0;
            var traceable = new Traceable();
            traceable.ToggleTracing(true, CancellationToken.None);
            traceable.History.OnCollectionChangedAsync += async (@event, cancellationToken) => notificationCount++;

            traceable.History.Push(new ReachedCheckpoint1());
            traceable.History.Push(new ReachedCheckpoint2());
            traceable.History.Push(new ReachedCheckpoint3());

            while(notificationCount < traceable.History.Count)
            {
                Thread.Sleep(1);
            }

            Assert.Equal(notificationCount, traceable.History.Count);
            traceable.ToggleTracing(false, CancellationToken.None);

        }
    }
}
