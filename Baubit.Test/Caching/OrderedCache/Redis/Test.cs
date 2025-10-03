using Baubit.Caching;
using Baubit.Collections;
using Baubit.DI;
using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Abstractions;
using RedisModuleConfig = Baubit.Caching.Redis.DI.Configuration;

namespace Baubit.Test.Caching.OrderedCache.Redis
{
    public class Test : ContainerTest<RedisBuilder, RedisContainer>
    {
        private RedisModuleConfig redisModuleConfig = new RedisModuleConfig
        {
            Host = "", // will be set at initialization for each test
            Port = 0, // will be set at initialization for each test
            SynchronizationOptions = new Baubit.Caching.Redis.SynchronizationOptions
            {
                GlobalTailIdKey = "baubit:metadata:tailId",
                LockKey = "baubit:metadata:lock",
                StreamKey = "baubit:metadata:stream",
                GroupName = "baubit:metadata:group",
                ConsumerName = $"{Environment.MachineName}:aee7f0f0-eb1d-4579-acb5-e5f0c2635b24"
            }
        };

        public Test(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {

        }

        protected override RedisBuilder Configure(RedisBuilder builder)
        {
            return builder.WithImage("redis:8.2.1");
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            var connectionString = Container.GetConnectionString();
            var connStrParts = connectionString.Split(":");
            redisModuleConfig = redisModuleConfig with { Host = connStrParts[0], Port = int.Parse(connStrParts[1]) };
        }
        [Fact]
        public async Task CanAwaitValues()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

            var nextEntryTask = redisCache.GetFutureFirstOrDefaultAsync();

            var addResult = redisCache.Add(Random.Shared.Next(0, 10), out var entry);
            Assert.True(addResult);

            var nextEntry = await nextEntryTask;

            Assert.Equal(entry.Value, nextEntry.Value);
        }

        [Fact]
        public async Task CanCancelGetNextAsync()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource = new CancellationTokenSource();

            var asyncGetter = redisCache.GetNextAsync(null, cancellationTokenSource.Token);

            await Task.Delay(500).ContinueWith(_ => cancellationTokenSource.Cancel());

            await Assert.ThrowsAsync<TaskCanceledException>(() => asyncGetter);
        }

        [Fact]
        public async Task ReaderCancellationsAreIsolated()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource1 = new CancellationTokenSource();
            var cancellationTokenSource2 = new CancellationTokenSource();

            var res2 = redisCache.GetNextAsync(null, cancellationTokenSource2.Token);

            var cancellationRunner = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                cancellationTokenSource1.Cancel();
                redisCache.Add(1, out _);
            });

            await Assert.ThrowsAsync<TaskCanceledException>(() => redisCache.GetNextAsync(null, cancellationTokenSource1.Token));

            var entry = await res2;
            Assert.NotNull(entry);
            Assert.Equal(1, entry.Value);
        }

        [Theory]
        //[InlineData(1000, 100, 5, 20)]
        //[InlineData(10, 1, 1, 1)]
        //[InlineData(10, 2, 1, 2)]
        //[InlineData(100, 2, 1, 2)]
        [InlineData(100, 10, 2, 8)]
        //[InlineData(1000, 100, 2, 8)]
        public async Task CanReadAndWriteSimultaneously(int numOfItems, int numOfReaders, int writerBatchMinSize, int writerBatchMaxSize)
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
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
