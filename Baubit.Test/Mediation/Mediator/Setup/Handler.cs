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

    public class Handler : IRequestHandler<Request, Response>
    {
        private Task<bool> mediationRunner;
        public Handler(IMediator mediator)
        {
            mediationRunner = mediator.RegisterHandlerAsync(this);
        }
        public async Task<Response> HandleNextAsync(Request request)
        {
            await Task.Yield();
            return new Response(request);
        }
    }
}
