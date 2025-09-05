using Baubit.Aggregation;
using Baubit.DI;
using Baubit.Test.Aggregation.Fast.Setup;

namespace Baubit.Test.Aggregation.Fast
{
    public class Test
    {
        static IFeature[] AggregationFeatures =
        [
            new Baubit.Logging.Features.F001(),
            new Baubit.Caching.InMemory.Features.F000<TestEvent>(),
            new Baubit.Aggregation.Features.F000<TestEvent>()
        ];
        [Theory]
        [InlineData(1000, 100)]
        public async Task CanReadAndWriteSimultaneously(int numOfEvents, int numOfConsumers)
        {
            var buildResult = ComponentBuilder<Aggregator<TestEvent>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(AggregationFeatures)).Bind(componentBuilder => componentBuilder.Build());
            Assert.True(buildResult.IsSuccess);
            var aggregator = buildResult.Value;
            var consumers = Enumerable.Range(0, numOfConsumers).Select(i => new EventConsumer(aggregator)).ToList();
            var events = Enumerable.Range(0, numOfEvents).Select(i => new TestEvent()).ToList();

            var parallelLoopResult = Parallel.ForEach(events, @event => { if (!aggregator.Publish(@event)) throw new Exception("<TBD>"); });
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
