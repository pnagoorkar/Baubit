using Baubit.Collections;
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Events;
using Baubit.Mediation;
using Baubit.Test.Events.Hub.Setup;
using System.Threading.Tasks;

namespace Baubit.Test.Events.Hub
{
    public class Test
    {
        [Theory]
        [InlineData(1000, 100)]
        public async Task  CanAggregate(int numOfNotifications, int numOfSubscribers)
        {
            var hub = ComponentBuilder<IHub>.Create()
                                            .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F001(),
                                                                                                    new Baubit.Caching.InMemory.Features.F000<object>()))
                                            .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Events.DI.Module(ConfigurationSource.Empty)))
                                            .Bind(componentBuilder => componentBuilder.Build())
                                            .Value;

            var cts = new CancellationTokenSource();

            var subscribers = Enumerable.Range(1, numOfSubscribers)
                                        .Select(_ =>
                                        {
                                            var subscriber = new Subscriber<int>(numOfNotifications);
                                            hub.SubscribeAsync(subscriber, cts.Token);
                                            return subscriber;
                                        })
                                        .ToList();

            var parallelLoopResult = Parallel.For(1, 
                                                  numOfNotifications + 1,
                                                  i =>
                                                  {
                                                      if (!hub.Publish(i))
                                                      {
                                                          throw new Exception("uh-oh!");
                                                      }
                                                  });

            Assert.Null(parallelLoopResult.LowestBreakIteration);

            await Task.WhenAll(subscribers.Select(sub => sub.AwaitLastItem(cts.Token)));

            cts.Cancel();
        }

        [Theory]
        [InlineData(1000)]
        public void CanMediate(int numOfRequests)
        {
            var hub = ComponentBuilder<IHub>.Create()
                                            .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F001(),
                                                                                                    new Baubit.Caching.InMemory.Features.F000<object>()))
                                            .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Events.DI.Module(ConfigurationSource.Empty)))
                                            .Bind(componentBuilder => componentBuilder.Build())
                                            .Value;

            var cts = new CancellationTokenSource();

            var handler = new Handler();
            hub.Subscribe(handler, cts.Token);

            var requests = Enumerable.Range(0, numOfRequests).Select(i => new Request()).ToList();
            var responses = new ConcurrentList<Response>();

            Parallel.ForEach(requests, (request) =>
            {
                var response = hub.Publish<Request, Response>(request);

                responses.Add(response);
            });

            Assert.Equal(numOfRequests, responses.Count);

            cts.Cancel();
        }

        [Theory]
        [InlineData(1000)]
        public async Task CanMediateAsync(int numOfRequests)
        {
            var hub = ComponentBuilder<IHub>.Create()
                                            .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F001(),
                                                                                                    new Baubit.Caching.InMemory.Features.F000<object>()))
                                            .Bind(componentBuilder => componentBuilder.WithModules(new Baubit.Events.DI.Module(ConfigurationSource.Empty)))
                                            .Bind(componentBuilder => componentBuilder.Build())
                                            .Value;

            var cts = new CancellationTokenSource();

            var handler = new Handler();
            hub.SubscribeAsync(handler, cts.Token);

            var requests = Enumerable.Range(0, numOfRequests).Select(i => new Request()).ToList();
            var responses = new ConcurrentList<Response>();

            await Parallel.ForEachAsync(requests, cts.Token, async (request, canToken) =>
            {
                var response = await hub.PublishAsyncAsync<Request, Response>(request, canToken);

                responses.Add(response);
            });

            Assert.Equal(numOfRequests, responses.Count);

            cts.Cancel();
        }
    }
}
