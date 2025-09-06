using Baubit.Mediation;

namespace Baubit.Test.Mediation.Mediator.Setup
{
    public class Request : IRequest
    {
        public long Id { get; init; }

        private static long idSeed = 0;

        public Request()
        {
            Id = Interlocked.Increment(ref idSeed);
        }
    }

    public class Response : IResponse
    {
        public long Id { get; init; }

        public long ForRequest { get; init; }

        private static long idSeed = 0;

        public Response(IRequest forRequest)
        {
            Id = Interlocked.Increment(ref idSeed);
            ForRequest = forRequest.Id;
        }
    }

    public class Handler : IRequestHandler<Request, Response>, IAsyncRequestHandler<Request, Response>
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task<bool> mediationRunner;
        public Handler(IMediator mediator)
        {
            mediator.RegisterHandler<Request, Response>(this, cancellationTokenSource.Token);
            mediationRunner = mediator.RegisterHandlerAsync(this, cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }

        public Response Handle(Request request)
        {
            return new Response(request);
        }


        public async Task<Response> HandleAsyncAsync(Request request)
        {
            await Task.Yield();
            return Handle(request);
        }

        public async Task<Response> HandleSyncAsync(Request request, CancellationToken cancellationToken = default)
        {
            return await HandleAsyncAsync(request);
        }
    }
}
