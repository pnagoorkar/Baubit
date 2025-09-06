using Baubit.Caching;
using Baubit.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Mediation
{
    public interface IRequest
    {
        public long Id { get; }
    }
    public interface IResponse
    {
        public long Id { get; }

        public long ForRequest { get; }
    }
    public interface IRequestHandler : IDisposable
    {

    }
    public interface IRequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest where TResponse : IResponse
    {
        TResponse Handle(TRequest request);
        Task<TResponse> HandleSyncAsync(TRequest request, CancellationToken cancellationToken = default);
    }
    public interface IAsyncRequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest where TResponse : IResponse
    {
        Task<TResponse> HandleAsyncAsync(TRequest request);
    }

    public interface IMediator
    {
        bool RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
        TResponse Publish<TRequest, TResponse>(TRequest request) where TRequest : IRequest where TResponse : IResponse;

        Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
        Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
        Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
    }

    public class HandlerNotRegisteredException : Exception { }

    public class Mediator : IMediator
    {
        private IList<IRequestHandler> handlers = new ConcurrentList<IRequestHandler>();
        private IList<IRequestHandler> asyncHandlers = new ConcurrentList<IRequestHandler>();
        private IServiceProvider serviceProvider;

        public Mediator(IServiceProvider serviceProvider, 
                        ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
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
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>);
            if (handler == null) throw new HandlerNotRegisteredException();

            return handler.Handle(request);
        }

        public async Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            var handler = (IRequestHandler<TRequest, TResponse>)handlers.FirstOrDefault(handler => handler is IRequestHandler<TRequest, TResponse>);
            if (handler == null) throw new HandlerNotRegisteredException();
            return await handler.HandleSyncAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request, 
                                                                       CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            if (!asyncHandlers.Any(handler => handler is IAsyncRequestHandler<TRequest, TResponse>)) throw new HandlerNotRegisteredException();

            var requestCache = serviceProvider.GetRequiredService<IOrderedCache<TRequest>>();
            var responseCache = serviceProvider.GetRequiredService<IOrderedCache<TResponse>>();

            var response = default(TResponse);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var responseProcessor = responseCache.EnumerateEntriesAsync(null, cts.Token)
                                                 .AggregateAsync(entry => 
                                                 {
                                                     if (entry.Value.ForRequest == request.Id)
                                                     {
                                                         response = entry.Value;
                                                         responseCache.Remove(entry.Id, out _);
                                                         cts.Cancel();
                                                     }
                                                     return true;
                                                 }, cts.Token);

            requestCache.Add(request, out var requestEntry);

            await responseProcessor.ConfigureAwait(false);

            requestCache.Remove(requestEntry.Id, out _);

            return response;
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
    }
}
