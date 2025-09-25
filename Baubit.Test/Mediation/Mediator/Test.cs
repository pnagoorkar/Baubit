using Baubit.Aggregation;
using Baubit.Collections;
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Mediation;
using Baubit.Test.Mediation.Mediator.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.Mediation.Mediator
{
    public class Test
    {
        [Theory]
        [InlineData(1000)]
        public async Task CanMediate(int numOfRequests)
        {
            Request.ResetSeed();
            Response.ResetSeed();
            var mediatorBuildResult = ComponentBuilder<IMediator>.Create()
                                                                 .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001(), new Baubit.Caching.InMemory.Features.F000<object>(), new Baubit.Caching.InMemory.Features.F000<Request>(), new Baubit.Caching.InMemory.Features.F000<Response>()]))
                                                                 .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Mediation.DI.Module(ConfigurationSource.Empty)))
                                                                 .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<ResponseLookup<Response>>()))
                                                                 .Bind(componentBuilder => componentBuilder.Build());

            Assert.True(mediatorBuildResult.IsSuccess);

            var mediator = mediatorBuildResult.Value;

            var handler = new Handler(mediator);

            var requests = Enumerable.Range(0, numOfRequests).Select(i => new Request()).ToList();
            var responses = new ConcurrentList<Response>();

            await Parallel.ForEachAsync(requests, async (request, cancellationToken) =>
            {
                var response = await mediator.PublishSyncAsync<Request, Response>(request);

                responses.Add(response);
            });

            Assert.Equal(numOfRequests, responses.Count);
        }
        [Theory]
        [InlineData(1000)]
        public async Task CanMediateAsync(int numOfRequests)
        {
            Request.ResetSeed();
            Response.ResetSeed();
            var mediatorBuildResult = ComponentBuilder<IMediator>.Create()
                                                                 .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001(), new Baubit.Caching.InMemory.Features.F000<object>(), new Baubit.Caching.InMemory.Features.F000<Request>(), new Baubit.Caching.InMemory.Features.F000<Response>()]))
                                                                 .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Mediation.DI.Module(ConfigurationSource.Empty)))
                                                                 .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<ResponseLookup<Response>>()))
                                                                 .Bind(componentBuilder => componentBuilder.Build());

            Assert.True(mediatorBuildResult.IsSuccess);

            var mediator = mediatorBuildResult.Value;

            var handler = new Handler(mediator);

            var requests = Enumerable.Range(0, numOfRequests).Select(i => new Request()).ToList();
            var responses = new ConcurrentList<Response>();

            await Parallel.ForEachAsync(requests, async (request, cancellationToken) =>
            {
                var response = await mediator.PublishAsyncAsync<Request, Response>(request);

                responses.Add(response);
            });

            Assert.Equal(numOfRequests, responses.Count);
        }

        [Theory]
        [InlineData(1000, 100)]
        public async Task MediatorCanAggregate(int numOfEvents, int numOfConsumers)
        {
            Request.ResetSeed();
            Response.ResetSeed();
            var buildResult = ComponentBuilder<IAggregator>.Create()
                                                           .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001(), 
                                                                                                                    new Baubit.Caching.InMemory.Features.F000<object>(), 
                                                                                                                    new Baubit.Caching.InMemory.Features.F000<Request>(), 
                                                                                                                    new Baubit.Caching.InMemory.Features.F000<Response>()]))
                                                           .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Mediation.DI.Module(ConfigurationSource.Empty)))
                                                           .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<ResponseLookup<Response>>()))
                                                           .Bind(componentBuilder => componentBuilder.Build());


            Assert.True(buildResult.IsSuccess);
            var aggregator = buildResult.Value;
            var consumers = Enumerable.Range(0, numOfConsumers).Select(i => new EventConsumer(aggregator)).ToList();
            var events = Enumerable.Range(0, numOfEvents).Select(i => new TestEvent()).ToList();

            var parallelLoopResult = Parallel.ForEach(events, @event => { if (!aggregator.Publish(@event, out _)) throw new Exception("<TBD>"); });
            Assert.Null(parallelLoopResult.LowestBreakIteration);

            var expectedNumOfReceipts = numOfEvents * numOfConsumers;
            var actualNumOfReceipts = events.Sum(@event => @event.Trace.Count);

            while (expectedNumOfReceipts != actualNumOfReceipts)
            {
                await Task.Delay(10);
                actualNumOfReceipts = events.Sum(@event => @event.Trace.Count);
            }

            Assert.Equal(expectedNumOfReceipts, actualNumOfReceipts);

        }
    }
}
