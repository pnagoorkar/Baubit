using Baubit.Aggregation;
using Baubit.Caching;
using Baubit.Collections;
using Baubit.Mediation.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Mediation
{
    /// <summary>
    /// Default implementation of <see cref="IMediator"/> that supports both synchronous
    /// request/response handling and an asynchronous pipeline backed by ordered caches.
    /// </summary>
    public class Mediator : Aggregator<object>, IMediator
    {
        private IList<IRequestHandler> handlers = new ConcurrentList<IRequestHandler>();
        private IList<IRequestHandler> asyncHandlers = new ConcurrentList<IRequestHandler>();

        /// <summary>
        /// Creates a new mediator instance.
        /// </summary>
        /// <param name="loggerFactory">Factory for creating loggers (reserved for future diagnostics).</param>
        public Mediator(IOrderedCache<object> cache,
                        ILoggerFactory loggerFactory) : base(cache, loggerFactory)
        {
        }

        /// <summary>
        /// Registers a synchronous handler instance for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
        /// The handler is automatically unregistered when <paramref name="cancellationToken"/> is canceled.
        /// </summary>
        /// <inheritdoc/>
        public bool RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler,
                                                         CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
                                                                                                        where TResponse : IResponse
        {
            if (handlers.Any(handler => handler is IRequestHandler<TRequest, TResponse>)) return false;
            handlers.Add(requestHandler);
            CancellationTokenRegistration registration = default;
            registration = cancellationToken.Register(() => { handlers.Remove(requestHandler); registration.Dispose(); });
            return true;
        }

        /// <summary>
        /// Publishes a request to a synchronous handler and returns its response.
        /// </summary>
        /// <inheritdoc/>
        /// <exception cref="HandlerNotRegisteredException">Thrown when no matching handler is registered.</exception>
        public TResponse Publish<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse>
                                                                        where TResponse : IResponse
        {
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>)!;
            if (handler == null) throw new HandlerNotRegisteredException();

            return handler.Handle(request);
        }

        /// <summary>
        /// Publishes a request to a synchronous handler and awaits the response.
        /// </summary>
        /// <inheritdoc/>
        /// <exception cref="HandlerNotRegisteredException">Thrown when no matching handler is registered.</exception>
        public async Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse
        {
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>)!;
            if (handler == null) throw new HandlerNotRegisteredException();
            return await handler.HandleSyncAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request,
                                                                            CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse
        {
            var trackedIndex = StartTracking();

            _cache.Add(request, out var entry);

            var retVal = default(TResponse);

            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _cache.EnumerateEntriesAsync(entry.Id, linkedCTS.Token)
                        .AggregateAsync(next =>
                        {
                            try
                            {
                                if (next.Value is TResponse response && response.ForRequest == request.Id)
                                {
                                    retVal = response;
                                    linkedCTS.Cancel();
                                }
                                return true;
                            }
                            finally
                            {
                                RecordRead(trackedIndex, next.Id);
                            }
                        }).ConfigureAwait(false);

            StopTracking(trackedIndex);

            return retVal!;
        }

        public async Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                                          CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse
        {
            asyncHandlers.Add(requestHandler);
            var trackedIndex = StartTracking();

            await _cache.EnumerateFutureEntriesAsync(cancellationToken)
                        .AggregateAsync(async next =>
                        {
                            try
                            {
                                if (next.Value is TRequest request)
                                {
                                    var response = await requestHandler.HandleAsyncAsync(request).ConfigureAwait(false);
                                    _cache.Add(response, out _);
                                }
                                return true;
                            }
                            finally
                            {
                                RecordRead(trackedIndex, next.Id);
                            }

                        }).ConfigureAwait(false);


            StopTracking(trackedIndex);

            return asyncHandlers.Remove(requestHandler);
        }
    }
}
