using Baubit.Observation;
using Baubit.Tasks;
using FluentResults;
using FluentResults.Extensions;

namespace Baubit.States
{
    public class StateWatcher<T> : ISubscriber<StateChanged<T>> where T : Enum
    {
        private T _watchFor;
        private ManualResetTask<T> _watcher = new ManualResetTask<T>();
        private CancellationTokenSource watcherCancellationTokenSource = new CancellationTokenSource();

        public StateWatcher(T watchFor)
        {
            _watchFor = watchFor;
            _watcher.Reset(watcherCancellationTokenSource.Token);
        }
        public Result OnCompleted()
        {
            return Result.Try(() => watcherCancellationTokenSource.Cancel()).Bind(() => Result.Ok());
        }

        public Result OnError(Exception error)
        {
            return _watcher.SetError(error);
        }

        public Result OnNext(StateChanged<T> value)
        {
            if (value.Current.Equals(_watchFor))
            {
                return _watcher.Set(value.Current);
            }
            return Result.Ok();
        }

        public async Task<Result> AwaitAsync(CancellationToken cancellationToken = default)
        {
            return await _watcher.WaitAsync().Bind(_ => Result.Ok());
        }

        public void Dispose()
        {

        }
    }
}
