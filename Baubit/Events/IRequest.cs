namespace Baubit.Events
{
    /// <summary>
    /// Marker for a mediator request. A request carries a unique <see cref="Id"/>
    /// that is used to correlate with a response's <see cref="IResponse.ForRequest"/>.
    /// </summary>
    public interface IRequest<TResponse> where TResponse : IResponse
    {
        /// <summary>
        /// Gets the unique identifier for this request.
        /// </summary>
        long Id { get; }
    }
}
