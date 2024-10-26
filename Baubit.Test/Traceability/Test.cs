using Baubit.Test.Traceability.Setup;

namespace Baubit.Test.Traceability
{
    public class Test
    {
        [Fact]
        public void CanObserveTraceables()
        {
            var notificationCount = 0;
            var traceable = new Traceable();
            traceable.History.OnCollectionChanged += @event => notificationCount++;

            traceable.History.Push(new ReachedCheckpoint1());
            traceable.History.Push(new ReachedCheckpoint2());
            traceable.History.Push(new ReachedCheckpoint3());

            while(notificationCount < traceable.History.Count)
            {
                Thread.Sleep(1);
            }

            Assert.Equal(notificationCount, traceable.History.Count);

        }
    }
}
