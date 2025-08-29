using Baubit.Caching;
using Baubit.Collections;
using Baubit.Observation;
using Baubit.States;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;

namespace Baubit.Aggregation
{
    public sealed class Aggregator<T> : IAggregator<T>
    {
        private bool disposedValue;
        private IList<Subscription<T>> _activeSubscriptions = new List<Subscription<T>>();
        private ConcurrentDictionary<long, ConcurrentList<Subscription<T>>> idSubMap = new ConcurrentDictionary<long, ConcurrentList<Subscription<T>>>();
        private IOrderedCache<T> _objectCache;
        private SubscriptionFactory<T> _subscriptionFactory;
        private ILogger<Aggregator<T>> _logger;
        private Task<Result> _dispatcher;
        private CancellationTokenSource dispatchCTS = new CancellationTokenSource();

        public Aggregator(IOrderedCache<T> objectCache, 
                          SubscriptionFactory<T> subscriptionFactory,
                          ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Aggregator<T>>();
            _objectCache = objectCache;
            _subscriptionFactory = subscriptionFactory;
            _dispatcher = DispatchAsync(dispatchCTS.Token);
        }

        public Result Publish(T item)
        {
            return PublishInternal(item).Bind(_ => Result.Ok());
        }

        public Task<Result> PublishAsync(T item, CancellationToken cancellationToken = default)
        {
            return PublishInternal(item).Bind(entry => AwaitDelivery(entry.Id, cancellationToken));
        }

        private Result<IEntry<T>> PublishInternal(T item)
        {
            if (disposedValue) return Result.Fail("Aggregator disposed!");
            if (!_activeSubscriptions.Any()) return Result.Ok();
            return _objectCache.Add(item);
        }

        private async Task<Result> DispatchAsync(CancellationToken cancellationToken = default)
        {
            return await _objectCache.EnumerateEntriesAsync(null, cancellationToken)
                                     .AggregateAsync(entry => TrackDelivery(entry.Id).Bind(() => Dispatch(entry.Id)))
                                     .ConfigureAwait(false);
        }

        private Result TrackDelivery(long id)
        {
            return Result.Try(() =>
            {
                idSubMap.TryAdd(id, new ConcurrentList<Subscription<T>>(_activeSubscriptions));
            });
        }

        private Result Dispatch(long id)
        {
            return _activeSubscriptions.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => next.Publish(id)));
        }

        public Result<IDisposable> Subscribe(ISubscriber<T> subscriber)
        {
            return Result.Try(() => _subscriptionFactory.Invoke(subscriber, OnDelivered, Unsubscribe))
                         .Bind(subscription => Result.Try(() => _activeSubscriptions.Add(subscription))
                                                     .Bind(() => Result.Ok<IDisposable>(subscription)));
        }

        private Result Unsubscribe(Subscription<T> subscription)
        {
            return Result.OkIf(_activeSubscriptions.Remove(subscription), "<TBD>")
                         .Bind(() => subscription.PendingDeliveryIds.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => OnDelivered(subscription, next))));
        }

        private Result OnDelivered(Subscription<T> sender, long id)
        {
            return Result.Try(() => idSubMap[id].Remove(sender))
                         .Bind(removed => Result.OkIf(removed, "<TBD>"))
                         .Bind(() => TryEvict(id));
        }

        private Result TryEvict(long id)
        {
            idSubMap.TryGetValue(id, out var subs);
            return subs?.Count > 0 ? Result.Ok() : Evict(id);
        }

        private Result Evict(long id)
        {
            return _objectCache.Remove(id)
                               .Bind(__ => Result.Try(() => { idSubMap.TryRemove(id, out _); }))
                               .Bind(() => SignalAwaiters(id));
        }

        private Result SignalAwaiters(long id)
        {
            return Result.Try(() =>
            {
                if (deliveryAwaiters.TryRemove(id, out var tcs))
                {
                    tcs.SetResult(Result.Ok());
                }
            });
        }

        private ConcurrentDictionary<long, TaskCompletionSource<Result>> deliveryAwaiters = new ConcurrentDictionary<long, TaskCompletionSource<Result>>();

        public Task<Result> AwaitDelivery(long id, CancellationToken cancellationToken = default)
        {
            return deliveryAwaiters.GetOrAdd(id, new TaskCompletionSource<Result>()).Task;
        }

        #region Dispose
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _activeSubscriptions.ToArray().Dispose();
                    _objectCache.Dispose();
                    idSubMap.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
