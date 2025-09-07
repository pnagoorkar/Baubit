namespace Baubit.Mediation
{
    /// <summary>
    /// Marker for a mediator response. Responses carry their own <see cref="Id"/> and
    /// identify the request they answer via <see cref="ForRequest"/>.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Gets the unique identifier for this response.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// Gets the identifier of the request that this response corresponds to.
        /// </summary>
        long ForRequest { get; }
    }
}
