using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baubit.Mediation
{
    /// <summary>
    /// Non-generic base interface for all request handlers.
    /// </summary>
    public interface IRequestHandler : IDisposable
    {
    }

    /// <summary>
    /// Defines a synchronous request handler for a specific request/response pair.
    /// Implementations may also expose an asynchronous method for convenience.
    /// </summary>
    /// <typeparam name="TRequest">The request type to handle.</typeparam>
    /// <typeparam name="TResponse">The response type to return.</typeparam>
    public interface IRequestHandler<TRequest, TResponse> : IRequestHandler
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        /// <summary>
        /// Handles the request synchronously and returns a response.
        /// </summary>
        /// <param name="request">The request payload.</param>
        /// <returns>The response.</returns>
        TResponse Handle(TRequest request);

        /// <summary>
        /// Handles the request asynchronously (while still using a synchronous handler implementation)
        /// and returns a response.
        /// </summary>
        /// <param name="request">The request payload.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes with the response.</returns>
        Task<TResponse> HandleSyncAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
