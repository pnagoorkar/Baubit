using System.Threading.Tasks;

namespace Baubit.Mediation
{
    /// <summary>
    /// Defines an asynchronous request handler for a specific request/response pair.
    /// Implementations are typically registered with an <see cref="IMediator"/> so that
    /// asynchronous requests published via <see cref="IMediator.PublishAsyncAsync{TRequest, TResponse}(TRequest, System.Threading.CancellationToken)"/>
    /// can be processed and a corresponding response produced.
    /// </summary>
    /// <typeparam name="TRequest">The request type to handle.</typeparam>
    /// <typeparam name="TResponse">The response type to return.</typeparam>
    public interface IAsyncRequestHandler<TRequest, TResponse> : IRequestHandler
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        /// <summary>
        /// Handles the request asynchronously and produces a response.
        /// </summary>
        /// <param name="request">The request payload.</param>
        /// <returns>A task that completes with the response.</returns>
        Task<TResponse> HandleAsyncAsync(TRequest request);
    }
}
