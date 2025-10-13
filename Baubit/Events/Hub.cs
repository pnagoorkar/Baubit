using Baubit.Caching;
using Baubit.Collections;
using Baubit.Identity;
using Baubit.Observation;
using Microsoft.Extensions.Logging;

namespace Baubit.Events
{
    public sealed class Hub : IHub
    {
        private bool disposedValue;
        private IList<IRequestHandler> _syncHandlers = new ConcurrentList<IRequestHandler>();
        private IList<IRequestHandler> _asyncHandlers = new ConcurrentList<IRequestHandler>();
        private IOrderedCache<object> _cache;
        private ILogger<Hub> _logger;
        private GuidV7Generator _idGenerator;

        public Hub(IOrderedCache<object> cache,
                   ILoggerFactory loggerFactory)
        {
            _cache = cache;
            _logger = loggerFactory.CreateLogger<Hub>();
            _idGenerator = GuidV7Generator.CreateNew();
        }

        public bool Publish(object notification)
        {
            return _cache.Add(notification, out _);
        }

        public TResponse Publish<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            var handler = _syncHandlers.SingleOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>);
            if (handler == null) throw new InvalidOperationException("No handler registered!");
            return ((IRequestHandler<TRequest, TResponse>)handler).Handle(request);
        }

        public async Task<TResponse> PublishAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            var handler = _syncHandlers.SingleOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>);
            if (handler == null) throw new InvalidOperationException("No handler registered!");
            return await ((IRequestHandler<TRequest, TResponse>)handler).HandleSyncAsync(request);
        }

        public async Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var enumerator = _cache.GetFutureAsyncEnumerator(linkedCTS.Token);
            var trackedRequest = new TrackedRequest<TRequest, TResponse>(_idGenerator.GetNext(), request);
            if (!_cache.Add(trackedRequest, out _)) throw new Exception("<TBD>");
            try
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (enumerator.Current.Value is TrackedResponse<TResponse> trackedResponse && trackedRequest.Id == trackedResponse.ForRequest)
                    {
                        return trackedResponse.Response;
                    }
                }
            }
            finally
            {
                linkedCTS.Cancel();
            }
            // the assumption is that the cancellation token must have been cancelled for the flow to have reached here without returning directly from the while above
            // if the code ever reaches here, that assumption must no longer be true
            throw new TaskCanceledException(string.Empty, null, cancellationToken);
        }

        public async Task<bool> SubscribeAsync<T>(ISubscriber<T> subscriber, CancellationToken cancellationToken = default)
        {
            var enumerator = _cache.GetFutureAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                if (enumerator.Current.Value is T tItem) subscriber.OnNextOrError(tItem);
            }
            return true;
        }

        public async IAsyncEnumerable<TType> EnumerateAsync<TType>([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerator = _cache.GetAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                if (enumerator.Current.Value is TType tItem) yield return tItem;
            }
        }

        public async IAsyncEnumerable<TType> EnumerateFutureAsync<TType>([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerator = _cache.GetFutureAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                if (enumerator.Current.Value is TType tItem) yield return tItem;
            }
        }

        public bool Subscribe<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler,
                                                   CancellationToken cancellationToken)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            if (_syncHandlers.Any(handler => handler is IRequestHandler<TRequest, TResponse>)) return false;
            _syncHandlers.Add(requestHandler);
            CancellationTokenRegistration registration = default;
            registration = cancellationToken.Register(() => { _syncHandlers.Remove(requestHandler); registration.Dispose(); });
            return true;
        }

        public async Task<bool> SubscribeAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            _asyncHandlers.Add(requestHandler);
            try
            {
                var enumerator = _cache.GetFutureAsyncEnumerator(cancellationToken);

                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (enumerator.Current.Value is TrackedRequest<TRequest, TResponse> trackedRequest)
                    {
                        var response = await requestHandler.HandleAsyncAsync(trackedRequest.Request).ConfigureAwait(false);
                        var trackedResponse = new TrackedResponse<TResponse>(trackedRequest.Id, response);
                        _cache.Add(trackedResponse, out _);
                    }
                }
            }
            finally
            {
                _asyncHandlers.Remove(requestHandler);
            }
            return true;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cache.Dispose();
                    _syncHandlers.Clear();
                    _asyncHandlers.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
