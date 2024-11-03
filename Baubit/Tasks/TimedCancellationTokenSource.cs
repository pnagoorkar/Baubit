using System.Text.Json.Serialization;

namespace Baubit.Tasks
{

    public class TimedCancellationTokenSource : CancellationTokenSource
    {
        private Task timer;

        public int MillisecondTimeout
        {
            get => Timeout.Milliseconds;
            init => Timeout = new TimeSpan(0, 0, 0, 0, value);
        }

        private TimeSpan timeOut;
        [JsonIgnore]
        public TimeSpan Timeout 
        {
            get => timeOut;
            init
            {
                timeOut = value;
                if (timeOut != System.Threading.Timeout.InfiniteTimeSpan)
                {
                    timer = new Task(() => { Thread.Sleep(timeOut); isCancellationRequested = true; });
                }
            }
        }

        private bool isCancellationRequested;

        public new bool IsCancellationRequested
        {
            get
            {
                if (timer.Status != TaskStatus.Running) timer.Start();
                return isCancellationRequested;
            }
        }

        public TimedCancellationTokenSource(int millisecondTimeout) => MillisecondTimeout = millisecondTimeout;
        public TimedCancellationTokenSource(TimeSpan? timeOut) => Timeout = timeOut ?? System.Threading.Timeout.InfiniteTimeSpan;

        public new void TryReset() => throw new NotImplementedException();
    }
}
