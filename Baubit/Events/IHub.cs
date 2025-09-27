using Baubit.Observation;

namespace Baubit.Events
{
    public interface IHub : IDisposable
    {
        public bool Publish(object notification);
        public TResponse Publish<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse> where TResponse : IResponse;
        public Task<TResponse> PublishAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse;

        public Task<bool> SubscribeAsync<T>(ISubscriber<T> subscriber, CancellationToken cancellationToken = default);
        public bool Subscribe<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler, 
                                                   CancellationToken cancellationToken) where TRequest : IRequest<TResponse> where TResponse : IResponse;
        public Task<bool> SubscribeAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                              CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> where TResponse : IResponse;

    }
}
