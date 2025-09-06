namespace Baubit.Mediation
{
    public interface IRequestHandler : IDisposable
    {

    }
    public interface IRequestHandler<TRequest, TResponse> : IRequestHandler where TRequest : IRequest where TResponse : IResponse
    {
        TResponse Handle(TRequest request);
        Task<TResponse> HandleSyncAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
