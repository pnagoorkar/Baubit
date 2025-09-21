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
    public class Mediator : IMediator
    {
        private IList<IRequestHandler> handlers = new ConcurrentList<IRequestHandler>();
        private IList<IRequestHandler> asyncHandlers = new ConcurrentList<IRequestHandler>();
        private IServiceProvider serviceProvider;
        private readonly ResolverCache _resolverCache;

        /// <summary>
        /// Creates a new mediator instance.
        /// </summary>
        /// <param name="serviceProvider">The application service provider used to resolve caches and lookups.</param>
        /// <param name="loggerFactory">Factory for creating loggers (reserved for future diagnostics).</param>
        public Mediator(IServiceProvider serviceProvider,
                        ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
            _resolverCache = new ResolverCache(serviceProvider);
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

        /// <summary>
        /// Publishes a request into the asynchronous pipeline and awaits the matching response.
        /// The request is added to the request cache, then the method waits for a matching
        /// response to appear in the response cache (via <see cref="ResponseLookup{TResponse}"/>).
        /// </summary>
        /// <inheritdoc/>
        /// <exception cref="HandlerNotRegisteredException">Thrown when no async handler is registered.</exception>
        public async Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request,
                                                                            CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse
        {
            if (!asyncHandlers.Any(handler => handler is IAsyncRequestHandler<TRequest, TResponse>)) throw new HandlerNotRegisteredException();

            var requestCache = _resolverCache.Cache<TRequest>();
            var responseCache = _resolverCache.Cache<TResponse>();

            var lookup = _resolverCache.Lookup<TResponse>();

            requestCache.Add(request, out var requestEntry);

            try
            {
                return await lookup.GetResponseAsync(request.Id, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                requestCache.Remove(requestEntry.Id, out _);
            }
        }

        /// <summary>
        /// Registers an asynchronous pipeline handler that listens on the request cache,
        /// transforms requests into responses, and writes responses to the response cache
        /// until <paramref name="cancellationToken"/> is canceled.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                                          CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse
        {
            asyncHandlers.Add(requestHandler);
            var requestCache = serviceProvider.GetRequiredService<IOrderedCache<TRequest>>();
            var responseCache = serviceProvider.GetRequiredService<IOrderedCache<TResponse>>();

            await requestCache.EnumerateEntriesAsync(null, cancellationToken)
                              .AggregateAsync(async entry =>
                              {
                                  var response = await requestHandler.HandleAsyncAsync(entry.Value).ConfigureAwait(false);
                                  return responseCache.Add(response, out _);
                              }, cancellationToken)
                              .ConfigureAwait(false);

            return asyncHandlers.Remove(requestHandler);
        }

        /// <summary>
        /// Local cache that resolves typed services (caches and lookups) and memoizes them.
        /// </summary>
        sealed class ResolverCache
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly ConcurrentDictionary<(Type open, Type arg), object> _map = new();

            /// <summary>
            /// Initializes a new instance of the resolver cache.
            /// </summary>
            /// <param name="serviceProvider">The root service provider.</param>
            public ResolverCache(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

            /// <summary>
            /// Gets the typed ordered cache for <typeparamref name="T"/> from DI.
            /// </summary>
            public IOrderedCache<T> Cache<T>() => (IOrderedCache<T>)Get(typeof(IOrderedCache<>), typeof(T));

            /// <summary>
            /// Gets the typed <see cref="ResponseLookup{TResponse}"/> from DI.
            /// </summary>
            public ResponseLookup<TResponse> Lookup<TResponse>() where TResponse : IResponse
            {
                return (ResponseLookup<TResponse>)Get(typeof(ResponseLookup<>), typeof(TResponse));
            }

            private object Get(Type openGeneric, Type arg)
            {
                if (!openGeneric.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("Must be an open generic type definition.", nameof(openGeneric));
                }

                return _map.GetOrAdd((openGeneric, arg), key =>
                {
                    var closed = key.open.MakeGenericType(key.arg);
                    return ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, closed);
                });
            }

        }
    }
}
