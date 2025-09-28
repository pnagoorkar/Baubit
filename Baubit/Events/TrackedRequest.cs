namespace Baubit.Events
{
    public class TrackedRequest<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : IResponse
    {
        public Guid Id { get; init; }
        public TRequest Request { get; init; }
        public TrackedRequest(Guid id, TRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
