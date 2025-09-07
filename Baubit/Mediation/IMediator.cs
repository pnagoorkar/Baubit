namespace Baubit.Mediation
{
    public interface IMediator
    {
        bool RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler, 
                                                  CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;

        Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                             CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;

        TResponse Publish<TRequest, TResponse>(TRequest request) where TRequest : IRequest where TResponse : IResponse;
        
        Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request, 
                                                              CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
        
        Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request, 
                                                               CancellationToken cancellationToken = default) where TRequest : IRequest where TResponse : IResponse;
    }
}
