
using Baubit.Aggregation;
using Baubit.DI;
using Baubit.Test.Aggregation.Aggregator.Setup;
using Baubit.Traceability;

namespace Baubit.Test.Aggregation.Aggregator
{
    public class Test
    {
        static IFeature[] AggregationFeatures =
        [
            new Baubit.Logging.Features.F001(),
            new Baubit.Caching.InMemory.Features.F000<TestEvent>(),
            new Baubit.Aggregation.Features.F000<TestEvent>(),
            new Baubit.Caching.InMemory.Features.F002<long>()
        ];
        [Theory]
        [InlineData(1000, 10)]
        public async Task Works(int numOfEvents, int numOfConsumers)
        {
            var buildResult = ComponentBuilder<Aggregator<TestEvent>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(AggregationFeatures)).Bind(componentBuilder => componentBuilder.Build());
            Assert.True(buildResult.IsSuccess);
            var aggregator = buildResult.Value;
            var consumers = Enumerable.Range(0, numOfConsumers).Select(i => new EventConsumer(aggregator)).ToList();
            var events = Enumerable.Range(0, numOfEvents).Select(i => new TestEvent()).ToList();

            var parallelLoopResult = Parallel.ForEach(events, @event => aggregator.Publish(@event).ThrowIfFailed());
            Assert.Null(parallelLoopResult.LowestBreakIteration);

            await aggregator.AwaitDelivery(numOfEvents);

            var expectedNumOfReceipts = numOfEvents * numOfConsumers;
            var actualNumOfReceipts = events.Sum(@event => @event.Trace.Count);

            Assert.Equal(expectedNumOfReceipts, actualNumOfReceipts);

        }
    }
}
