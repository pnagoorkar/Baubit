using Baubit.Logging.Telemetry;

namespace WebApp
{
    public class MyComponent
    {
        public string SomeStringValue { get; set; }
        private readonly ActivityTracker _activityTracker;

        public MyComponent(string someStringValue, ActivityTracker activityTracker)
        {
            SomeStringValue = someStringValue;
            _activityTracker = activityTracker;
        }

        public string DoSomething()
        {
            var activity = _activityTracker.StartTracking($"{nameof(MyComponent.DoSomething)}", System.Diagnostics.ActivityKind.Server);
            activity.Start();

            Thread.Sleep(Random.Shared.Next(20, 40));

            _activityTracker.StopTracking(activity);
            return SomeStringValue;
        }
    }
}
