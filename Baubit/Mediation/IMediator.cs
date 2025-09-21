using Baubit.Aggregation;
using System.Threading;
using System.Threading.Tasks;

namespace Baubit.Mediation
{
    /// <summary>
    /// Coordinates request/response messaging by routing requests to registered handlers
    /// and returning responses. Supports synchronous handlers and asynchronous pipeline handlers.
    /// </summary>
    public interface IMediator : IAggregator
    {
        /// <summary>
        /// Registers a synchronous handler for the given request/response types.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="requestHandler">The handler instance to register.</param>
        /// <param name="cancellationToken">
        /// A token used to automatically unregister the handler when canceled.
        /// </param>
        /// <returns>
        /// <c>true</c> if registration succeeded; <c>false</c> if a handler for this pair already exists.
        /// </returns>
        bool RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> requestHandler,
                                                  CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse;

        /// <summary>
        /// Registers an asynchronous handler for the given request/response types.
        /// The mediator will connect this handler to the request and response caches
        /// and run a streaming pipeline that transforms requests into responses.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="requestHandler">The asynchronous handler instance to register.</param>
        /// <param name="cancellationToken">
        /// A token used to stop the handler pipeline and unregister it.
        /// </param>
        /// <returns>
        /// A task that resolves to <c>true</c> when the handler has been unregistered;
        /// <c>false</c> otherwise.
        /// </returns>
        Task<bool> RegisterHandlerAsync<TRequest, TResponse>(IAsyncRequestHandler<TRequest, TResponse> requestHandler,
                                                             CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse;

        /// <summary>
        /// Publishes a request to a synchronous handler and returns its response.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to publish.</param>
        /// <returns>The response from the registered handler.</returns>
        /// <exception cref="Exceptions.HandlerNotRegisteredException">
        /// Thrown if no handler is registered for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
        /// </exception>
        TResponse Publish<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse;

        /// <summary>
        /// Publishes a request to a synchronous handler and awaits the response.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to publish.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes with the response.</returns>
        /// <exception cref="Exceptions.HandlerNotRegisteredException">
        /// Thrown if no handler is registered for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
        /// </exception>
        Task<TResponse> PublishSyncAsync<TRequest, TResponse>(TRequest request,
                                                              CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse;

        /// <summary>
        /// Publishes a request into the asynchronous pipeline. The response is produced by
        /// an <see cref="IAsyncRequestHandler{TRequest, TResponse}"/> registered via
        /// <see cref="RegisterHandlerAsync{TRequest, TResponse}(IAsyncRequestHandler{TRequest, TResponse}, CancellationToken)"/>,
        /// and delivered when available.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to publish.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes with the response produced by the async handler.</returns>
        /// <exception cref="Exceptions.HandlerNotRegisteredException">
        /// Thrown if no async handler is registered for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
        /// </exception>
        Task<TResponse> PublishAsyncAsync<TRequest, TResponse>(TRequest request,
                                                               CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse;
    }
}
