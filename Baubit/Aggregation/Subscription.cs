using Baubit.Caching;
using Baubit.Observation;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Baubit.Aggregation
{
    public delegate Subscription<T> SubscriptionFactory<T>(ISubscriber<T> subscriber, Func<Subscription<T>, long, Result> postDeliveryHandler, Func<Subscription<T>, Result> disposeHandler);
    public class Subscription<T> : IDisposable
    {
        private static long idSeed = 0;
        public long Id { get; init; }
        public IReadOnlyCollection<long> PendingDeliveryIds { get => _pendingItemIds.EnumerateValues().ToList().AsReadOnly(); }
        private bool disposedValue;
        private ISubscriber<T> subscriber;
        private IOrderedCache<long> _pendingItemIds;
        private IOrderedCache<T> _objectCache;
        private Func<Subscription<T>, Result> _disposeHandler;
        private Func<Subscription<T>, long, Result> _postDeliveryHandler;
        private ILogger<Subscription<T>> _logger;
        private Task deliveryRunner;

        public Subscription(ISubscriber<T> current,
                            Func<Subscription<T>, long, Result> postDeliveryHandler,
                            Func<Subscription<T>, Result> disposeHandler,
                            IOrderedCache<T> objectCache,
                            IOrderedCache<long> pendingItemIds,
                            ILoggerFactory loggerFactory)
        {
            Id = Interlocked.Increment(ref idSeed);
            _logger = loggerFactory.CreateLogger<Subscription<T>>();
            _objectCache = objectCache;
            _pendingItemIds = pendingItemIds;
            subscriber = current;
            _disposeHandler = disposeHandler;
            _postDeliveryHandler = postDeliveryHandler;
            deliveryRunner = DeliverAsync();
        }

        internal Result Publish(long id)
        {
            return _pendingItemIds.Add(id).Bind(_ => Result.Ok());
        }

        private async Task<Result> DeliverAsync(CancellationToken cancellationToken = default)
        {
            return await _pendingItemIds.EnumerateValuesAsync(null, cancellationToken)
                                        .AggregateAsync(DeliverNext, cancellationToken)
                                        .ConfigureAwait(false);
        }

        private Result DeliverNext(long id)
        {
            return _objectCache.GetValue(id)
                               .Bind(Deliver)
                               .Bind(() => OnItemDelivered(id));
        }

        private Result Deliver(T item)
        {
            return subscriber.OnNextOrError(item);
        }

        private Result OnItemDelivered(long id)
        {
            return _postDeliveryHandler(this, id).Bind(() => _pendingItemIds.Remove(id).Bind(_ => Result.Ok()));
        }

        #region Dispose
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var result = _disposeHandler(this).Bind(() => _pendingItemIds.Clear()).Bind(() => subscriber.OnCompleted());
                    _logger.LogCritical($"Subscription {Id} disposal unsuccessful {Environment.NewLine} {result.UnwrapReasons().ValueOrDefault}");
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
