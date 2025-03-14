using Baubit.Logging.Telemetry;

namespace WebApp
{
    public class MyComponent
    {
        public string SomeStringValue { get; set; }
        private readonly PerfTracker _perfTracker;

        public MyComponent(string someStringValue, PerfTracker perfTracker)
        {
            SomeStringValue = someStringValue;
            _perfTracker = perfTracker;
        }

        public string DoSomething()
        {
            var activity = _perfTracker.StartTracking($"{nameof(MyComponent.DoSomething)}", System.Diagnostics.ActivityKind.Server);
            activity.Start();

            Thread.Sleep(Random.Shared.Next(20, 40));

            _perfTracker.StopTracking(activity);
            return SomeStringValue;
        }
    }
}
