using Baubit.Caching;
using Baubit.Collections;
using Baubit.Observation;
using Baubit.Tasks;
using Baubit.Traceability;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Baubit.States
{
    public delegate State<T> StateFactory<T>() where T : Enum;

    public interface IState<T> : IPublisher<StateChanged<T>>, IDisposable where T : Enum
    {
        public T Current { get; }
    }
    public sealed class State<T> : IPublisher<StateChanged<T>>, IDisposable where T : Enum
    {
        public T Current { get => _cache.GetLast().Bind(entry => entry == null ? Result.Ok(default(T)) : Result.Ok(entry.Value)).Value; }

        IOrderedCache<T> _cache;
        IOrderedCache<StateChanged<T>> _changeCache;
        Task<Result> eventGenerator = null;
        Task<Result> eventDispatcher = null;
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ILogger<State<T>> _logger;

        public State(IOrderedCache<T> cache,
                     IOrderedCache<StateChanged<T>> changeCache,
                     ILoggerFactory loggerFactory)
        {
            _cache = cache;
            _changeCache = changeCache;
            _logger = loggerFactory.CreateLogger<State<T>>();
            eventDispatcher = Task.Run(DispatchEventsAsync).ContinueWith(completedTask => LogDispatcherEndStatus(completedTask));
            eventGenerator = Task.Run(GenerateChangeEventsAsync).ContinueWith(completedTask => LogEventGeneratorEndStatus(completedTask));
        }

        public Result Set(T value) => _cache.Add(value).Bind(_ => Result.Ok());

        IList<ISubscriber<StateChanged<T>>> _subscribers = new ConcurrentList<ISubscriber<StateChanged<T>>>();
        private bool disposedValue;

        public Result<IDisposable> Subscribe(ISubscriber<StateChanged<T>> subscriber)
        {
            return Result.Try(() => _subscribers.Add(subscriber))
                         .Bind(() => Result.Try<IDisposable>(() => new ChangeSubscription<T>(subscriber, _subscribers)));
        }

        public async Task<Result> AwaitAsync(T targetState, CancellationToken cancellationToken = default)
        {
            try
            {
                long? tailId = null;
                _changeCache.GetLast().Bind(tailEntry => Result.Try(() => tailId = tailEntry?.Id));
                await foreach (var entryResult in _changeCache.ReadAsync<IOrderedCache<StateChanged<T>>, StateChanged<T>>(tailId, cancellationToken))
                {
                    var result = entryResult.Bind(entry => Result.Try(() => entry.Value.Current.Equals(targetState)));
                    if (result.IsSuccess) return Result.Ok();
                }
                return Result.Fail(""); //This should never be hit. The above loop will only break if the caller cancels via the cancellation token
            }
            catch (TaskCanceledException)
            {
                return Result.Fail("Cancelled");
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private async Task<Result> GenerateChangeEventsAsync()
        {
            try
            {
                await foreach (var readResult in _cache.ReadAsync<IOrderedCache<T>, T>(null, cancellationTokenSource.Token))
                {
                    readResult.Bind(entry => Result.Try(() => new StateChanged<T>() { Current = entry.Value }))
                              .Bind(@event => _changeCache.Add(@event))
                              .ThrowIfFailed();
                }
            }
            catch (TaskCanceledException)
            {
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            return Result.Ok();
        }

        private async Task<Result> DispatchEventsAsync()
        {
            try
            {
                await foreach (var change in _changeCache.ReadAsync<IOrderedCache<StateChanged<T>>, StateChanged<T>>(null, cancellationTokenSource.Token))
                {
                    change.Bind(entry => Result.Try(() => Parallel.ForEach(_subscribers, subscriber => subscriber.OnNextOrError(entry.Value))));
                }
                Parallel.ForEach(_subscribers, subscriber => subscriber.OnCompleted());
            }
            catch (TaskCanceledException)
            {
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
            return Result.Ok();
        }

        private Result LogDispatcherEndStatus(Task<Result> completedTask)
        {
            return Result.Try(() =>
            {
                if (completedTask.Result.IsSuccess)
                {
                    _logger.LogInformation("Dispatcher finished gracefully");
                }
                else
                {
                    _logger.LogError($"Dispatcher finished in error:{Environment.NewLine}{completedTask.Result.UnwrapReasons().ValueOrDefault}");
                }
            }).Bind(() => completedTask.Result);
        }

        private Result LogEventGeneratorEndStatus(Task<Result> completedTask)
        {
            return Result.Try(() =>
            {
                if (completedTask.Result.IsSuccess)
                {
                    _logger.LogInformation("Event generator finished gracefully");
                }
                else
                {
                    _logger.LogError($"Event generator finished in error:{Environment.NewLine}{completedTask.Result.UnwrapReasons().ValueOrDefault}");
                }
            }).Bind(() => completedTask.Result);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Cancel();
                    eventGenerator.Wait(true);
                    eventDispatcher.Wait(true);
                    _subscribers.Clear();
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
