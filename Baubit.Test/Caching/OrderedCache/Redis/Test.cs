using Baubit.Caching;
using Baubit.Collections;
using Baubit.DI;
using RedisModuleConfig = Baubit.Caching.Redis.DI.Configuration;

namespace Baubit.Test.Caching.OrderedCache.Redis
{
    public class Test
    {
        public static RedisModuleConfig RedisModuleConfig = new RedisModuleConfig
        {
            Host = "127.0.0.1",
            Port = 6379,
            SynchronizationOptions = new Baubit.Caching.Redis.SynchronizationOptions
            {
                GlobalTailIdKey = "baubit:metadata:tailId",
                LockKey = "baubit:metadata:lock",
                StreamKey = "baubit:metadata:stream",
                GroupName = "baubit:metadata:group",
                ConsumerName = $"{Environment.MachineName}:aee7f0f0-eb1d-4579-acb5-e5f0c2635b24"
            }
        };
        [Fact]
        public async Task CanAwaitValues()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(RedisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

            var nextEntryTask = redisCache.GetFutureFirstOrDefaultAsync();

            var addResult = redisCache.Add(Random.Shared.Next(0, 10), out var entry);
            Assert.True(addResult);

            var nextEntry = await nextEntryTask;

            Assert.Equal(entry.Value, nextEntry.Value);
        }

        [Theory]
        //[InlineData(1000, 100, 5, 20)]
        [InlineData(10, 1, 1, 1)]
        [InlineData(10, 2, 1, 2)]
        [InlineData(100, 2, 1, 2)]
        [InlineData(100, 10, 2, 8)]
        //[InlineData(1000, 100, 2, 8)]
        public async Task CanReadAndWriteSimultaneously(int numOfItems, int numOfReaders, int writerBatchMinSize, int writerBatchMaxSize)
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(RedisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

            var readCTS = new CancellationTokenSource();
            var readCount = 0;
            int expectedNumOfReads = numOfItems * numOfReaders;
            ConcurrentList<Task<bool>> readerTasks = new ConcurrentList<Task<bool>>();
            var reader = async (IAsyncEnumerator<IEntry<int>> enumerator) =>
            {
                while (!readCTS.IsCancellationRequested && await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (Interlocked.Increment(ref readCount) == expectedNumOfReads) readCTS.Cancel();
                }
                return true;
            };
            var readerBurst = () => { Parallel.For(0, numOfReaders, i => readerTasks.Add(reader(redisCache.GetAsyncEnumerator(readCTS.Token)))); };
            var writerBurst = (int batchSize) => Parallel.For(0, batchSize, i => redisCache.Add(i, out _));

            readerBurst();

            var insertedCount = 0;
            while (insertedCount < numOfItems)
            {
                var batchSize = Math.Min(numOfItems - insertedCount, Random.Shared.Next(writerBatchMinSize, writerBatchMaxSize));
                writerBurst(batchSize);
                Thread.Sleep(10);
                insertedCount += batchSize;
            }
            await Task.WhenAll(readerTasks);
        }
    }
}
