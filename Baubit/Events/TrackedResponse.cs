namespace Baubit.Events
{
    public class TrackedResponse<TResponse> where TResponse : IResponse
    {
        public Guid ForRequest { get; init; }
        public TResponse Response { get; init; }
        public TrackedResponse(Guid forRequest, TResponse response)
        {
            ForRequest = forRequest;
            Response = response;
        }
    }
}
