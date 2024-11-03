namespace Baubit.Tasks
{
    public class CompositeCancellationTokenSource : CancellationTokenSource
    {
        public TimedCancellationTokenSource TimedCancellationTokenSource { get; init; }
        public CancellationToken CancellationTokenOfUnknownSource { get; init; }

        public new bool IsCancellationRequested
        {
            get
            {
                var result = CancellationTokenOfUnknownSource.IsCancellationRequested || CancellationTokenOfUnknownSource.IsCancellationRequested;
                if (result) CancelledDueToTimeOut = CancelledDueToTimeOut ?? !CancellationTokenOfUnknownSource.IsCancellationRequested;
                return result;
            }
        }

        public bool? CancelledDueToTimeOut { get; private set; }

        public CompositeCancellationTokenSource(TimeSpan? timeOut, CancellationToken cancellationTokenOfUnknownSource)
        {
            TimedCancellationTokenSource = new TimedCancellationTokenSource(timeOut);
            CancellationTokenOfUnknownSource = cancellationTokenOfUnknownSource;
        }

        public CompositeCancellationTokenSource(int millisecondTimeout, CancellationToken cancellationTokenOfUnknownSource) : this(new TimeSpan(0, 0, 0, 0, millisecondTimeout), cancellationTokenOfUnknownSource)
        {

        }

        public new void TryReset() => throw new NotImplementedException();
    }
}
