namespace Baubit.Mediation
{
    public interface IAsyncRequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest where TResponse : IResponse
    {
        Task<TResponse> HandleAsyncAsync(TRequest request);
    }
}
