using Baubit.Caching;
using Baubit.Collections;
using Baubit.Tasks;
using Baubit.Traceability;
using FluentResults;
using System;
using System.Threading;

namespace Baubit.States
{
    public sealed class State<T> : IObservable<StateChanged<T>>, IDisposable where T : Enum
    {
        public T Current { get => _cache.GetLast().Bind(entry => entry == null ? Result.Ok(default(T)) : Result.Ok(entry.Value)).Value; }

        IOrderedCache<T> _cache;
        IOrderedCache<StateChanged<T>> _changeCache;
        Task<Result> eventDispatcher = null;
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public State(IOrderedCache<T> cache, 
                     IOrderedCache<StateChanged<T>> changeCache)
        {
            _cache = cache;
            _changeCache = changeCache;
            eventDispatcher = Task.Run(DispatchEventsAsync);
        }

        public Result Set(T value) => _cache.Add(value).Bind(_ => Result.Ok());

        IList<IObserver<StateChanged<T>>> _observers = new ConcurrentList<IObserver<StateChanged<T>>>();
        private bool disposedValue;

        public IDisposable Subscribe(IObserver<StateChanged<T>> observer)
        {
            return Result.Try(() => _observers.Add(observer))
                         .Bind(() => Result.Try(() => new ChangeSubscription<T>(observer, _observers)))
                         .ThrowIfFailed()
                         .Value;
        }

        private async Task<Result> DispatchEventsAsync()
        {
            try
            {
                await foreach (var change in _changeCache.ReadAsync<IOrderedCache<StateChanged<T>>, StateChanged<T>>(cancellationTokenSource.Token))
                {
                    change.Bind(entry => Result.Try(() => Parallel.ForEach(_observers, observer => observer.OnNext(entry.Value))));
                }
                Parallel.ForEach(_observers, observer => observer.OnCompleted());
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            return Result.Ok();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Cancel();
                    eventDispatcher.Wait(true);
                    _observers.Clear();
                    _changeCache.Dispose();
                    _cache.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
