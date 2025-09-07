using Baubit.Caching;
using Baubit.Collections;
using Baubit.Mediation.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Mediation
{
    public class Mediator : IMediator
    {
        private IList<IRequestHandler> handlers = new ConcurrentList<IRequestHandler>();
        private IList<IRequestHandler> asyncHandlers = new ConcurrentList<IRequestHandler>();
        private IServiceProvider serviceProvider;
        private readonly ResolverCache _resolverCache;
        public Mediator(IServiceProvider serviceProvider,
                        ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
            _resolverCache = new ResolverCache(serviceProvider);
        }

        public bool RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler,
                                                         CancellationToken cancellationToken = default) where TRequest : IRequest
                                                                                                        where TResponse : IResponse
        {
            if (handlers.Any(handler => handler is IRequestHandler<TRequest, TResponse>)) return false;
            handlers.Add(requestHandler);
            CancellationTokenRegistration registration = default;
            registration = cancellationToken.Register(() => { handlers.Remove(requestHandler); registration.Dispose(); });
            return true;
        }

        public TResponse Publish<TRequest, TResponse>(TRequest request) where TRequest : IRequest
                                                                        where TResponse : IResponse
        {
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>)!;
            if (handler == null) throw new HandlerNotRegisteredException();

            return handler.Handle(request);
        }

        public async Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>)!;
            if (handler == null) throw new HandlerNotRegisteredException();
            return await handler.HandleSyncAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request,
                                                                            CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            if (!asyncHandlers.Any(handler => handler is IAsyncRequestHandler<TRequest, TResponse>)) throw new HandlerNotRegisteredException();

            var requestCache = _resolverCache.Cache<TRequest>();
            var responseCache = _resolverCache.Cache<TResponse>();

            var lookup = _resolverCache.Lookup<TResponse>();

            requestCache.Add(request, out var requestEntry);

            try
            {
                return await lookup.GetResponseAsync(request.Id, cancellationToken);
            }
            finally
            {
                requestCache.Remove(requestEntry.Id, out _);
            }
        }

        public async Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                                          CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            asyncHandlers.Add(requestHandler);
            var requestCache = serviceProvider.GetRequiredService<IOrderedCache<TRequest>>();
            var responseCache = serviceProvider.GetRequiredService<IOrderedCache<TResponse>>();

            await requestCache.EnumerateEntriesAsync(null, cancellationToken)
                              .AggregateAsync(async entry =>
                              {
                                  var response = await requestHandler.HandleAsyncAsync(entry.Value);
                                  return responseCache.Add(response, out _);
                              }, cancellationToken)
                              .ConfigureAwait(false);

            return asyncHandlers.Remove(requestHandler);
        }
        sealed class ResolverCache
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly ConcurrentDictionary<(Type open, Type arg), object> _map = new();

            public ResolverCache(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

            public IOrderedCache<T> Cache<T>() => (IOrderedCache<T>)Get(typeof(IOrderedCache<>), typeof(T));

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
