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
    public interface IRequestHandler
    {

    }
    public interface IRequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest where TResponse : IResponse
    {
        Task<TResponse> HandleNextAsync(TRequest request);
    }

    public interface IMediator
    {
        Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler) where TRequest : IRequest where TResponse : IResponse;
        Task<TResponse> PublishAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
    }

    public class HandlerNotRegisteredException : Exception { }

    public class Mediator : IMediator
    {
        private IList<IRequestHandler> handlers = new ConcurrentList<IRequestHandler>();
        private IServiceProvider serviceProvider;

        public Mediator(IServiceProvider serviceProvider, 
                        ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<TResponse> PublishAsync<TRequest, TResponse>(TRequest request, 
                                                                       CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse
        {
            if (!handlers.Any(handler => handler is IRequestHandler<TRequest, TResponse>)) throw new HandlerNotRegisteredException();

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
                                                 });

            requestCache.Add(request, out var requestEntry);

            await responseProcessor.ConfigureAwait(false);

            requestCache.Remove(requestEntry.Id, out _);

            return response;
        }

        public async Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler) where TRequest : IRequest where TResponse : IResponse
        {
            handlers.Add(requestHandler);
            var requestCache = serviceProvider.GetRequiredService<IOrderedCache<TRequest>>();
            var responseCache = serviceProvider.GetRequiredService<IOrderedCache<TResponse>>();

            await requestCache.EnumerateEntriesAsync()
                              .AggregateAsync(async entry =>
                              {
                                  var response = await requestHandler.HandleNextAsync(entry.Value);
                                  return responseCache.Add(response, out _);
                              })
                              .ConfigureAwait(false);

            return handlers.Remove(requestHandler);
        }
    }
}
