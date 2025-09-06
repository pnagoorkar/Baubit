using Baubit.Collections;
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Mediation;
using Baubit.Test.Mediation.Mediator.Setup;

namespace Baubit.Test.Mediation.Mediator
{
    public class Test
    {
        [Theory]
        [InlineData(1000)]
        public async Task CanMediate(int numOfRequests)
        {
            var mediatorBuildResult = ComponentBuilder<IMediator>.Create()
                                                                 .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001(), new Baubit.Caching.InMemory.Features.F000<Request>(), new Baubit.Caching.InMemory.Features.F000<Response>()]))
                                                                 .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Mediation.DI.Module(ConfigurationSource.Empty)))
                                                                 .Bind(componentBuilder => componentBuilder.Build());

            Assert.True(mediatorBuildResult.IsSuccess);

            var mediator = mediatorBuildResult.Value;

            var handler = new Handler(mediator);

            var requests = Enumerable.Range(0, numOfRequests).Select(i => new Request()).ToList();
            var responses = new ConcurrentList<Response>();

            await Parallel.ForEachAsync(requests, async (request, cancellationToken) =>
            {
                var response = await mediator.PublishAsync<Request, Response>(request);

                responses.Add(response);
            });            
            
            Assert.Equal(numOfRequests, responses.Count);
        }
    }
}
