namespace Baubit.Mediation
{
    /// <summary>
    /// Marker for a mediator request. A request carries a unique <see cref="Id"/>
    /// that is used to correlate with a response's <see cref="IResponse.ForRequest"/>.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// Gets the unique identifier for this request.
        /// </summary>
        long Id { get; }
    }
}
