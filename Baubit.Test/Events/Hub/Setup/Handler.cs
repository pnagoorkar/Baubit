using Baubit.Events;

namespace Baubit.Test.Events.Hub.Setup
{
    public class Request : IRequest<Response>
    {
        public long Id { get; init; }

        private static long idSeed = 0;

        public Request()
        {
            Id = Interlocked.Increment(ref idSeed);
        }

        public static void ResetSeed()
        {
            idSeed = 0;
        }
    }

    public class Response : IResponse
    {
        public long Id { get; init; }

        public long ForRequest { get; init; }

        private static long idSeed = 0;

        public Response(IRequest<Response> forRequest)
        {
            Id = Interlocked.Increment(ref idSeed);
            ForRequest = forRequest.Id;
        }

        public static void ResetSeed()
        {
            idSeed = 0;
        }
    }
    public class Handler : IAsyncRequestHandler<Request, Response>, IRequestHandler<Request, Response>
    {
        public Response Handle(Request request)
        {
            return new Response(request);
        }

        public Task<Response> HandleAsyncAsync(Request request)
        {
            return Task.FromResult(Handle(request));
        }

        public Task<Response> HandleSyncAsync(Request request, CancellationToken cancellationToken = default)
        {
            return HandleAsyncAsync(request);
        }
        public void Dispose()
        {

        }
    }
}
