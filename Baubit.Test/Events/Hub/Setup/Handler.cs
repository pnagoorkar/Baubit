using Baubit.Events;

namespace Baubit.Test.Events.Hub.Setup
{
    public class Request : IRequest<Response>
    {

    }

    public class Response : IResponse
    {

    }
    public class Handler : IAsyncRequestHandler<Request, Response>, IRequestHandler<Request, Response>
    {
        public Response Handle(Request request)
        {
            return new Response();
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
