using Baubit.Caching;
using Baubit.Collections;
using Baubit.DI;
using System.Reflection.Metadata.Ecma335;
using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Abstractions;
using RedisModuleConfig = Baubit.Caching.Redis.DI.Configuration;

namespace Baubit.Test.Caching.OrderedCache.Redis
{
    public sealed class RedisContainerFixture(IMessageSink messageSink) : ContainerFixture<RedisBuilder, RedisContainer>(messageSink)
    {
        protected override RedisBuilder Configure(RedisBuilder builder)
        {
            return builder.WithImage("redis:8.2.1");
        }
    }
    public class Test : IClassFixture<RedisContainerFixture>
    {
        private RedisModuleConfig redisModuleConfig = new RedisModuleConfig
        {
            Host = "127.0.0.1", // will be set at initialization for each test
            Port = 6379, // will be set at initialization for each test
            RedisSettings = new Baubit.Caching.Redis.RedisSettings
            {
                AppName = "baubit.test",
                ResumeSession = true
            }
        };

        public Test(RedisContainerFixture fixture)
        {
            var connectionString = fixture.Container.GetConnectionString();
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
            redisCache.Dispose();
        }

        [Theory]
        [InlineData(1)]
        public async Task CanEvictAfterEveryX(int x)
        {
            var configuration = redisModuleConfig with { CacheConfiguration = new Baubit.Caching.Configuration { EvictAfterEveryX = x } };

            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(configuration, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

            var enumerator = redisCache.GetAsyncEnumerator();

            redisCache.Add(Random.Shared.Next(), out var entry);
            Assert.Equal(redisCache.Count, 1);

            Assert.Null(enumerator.Current);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

            redisCache.Add(Random.Shared.Next(), out entry);
            Assert.Equal(redisCache.Count, 1);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

            redisCache.Add(Random.Shared.Next(), out entry);
            Assert.Equal(redisCache.Count, 1);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

            redisCache.Dispose();
        }

        [Fact]
        public async Task CanCancelGetNextAsync()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

            var cancellationTokenSource = new CancellationTokenSource();

            var asyncGetter = redisCache.GetNextAsync(null, cancellationTokenSource.Token);

            await Task.Delay(500).ContinueWith(_ => cancellationTokenSource.Cancel());

            await Assert.ThrowsAsync<TaskCanceledException>(() => asyncGetter);
            redisCache.Dispose();
        }

        [Fact]
        public async Task ReaderCancellationsAreIsolated()
        {
            var redisCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(redisModuleConfig, [], [])]))
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            redisCache.Clear();

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
            redisCache.Dispose();
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
            redisCache.Dispose();
        }

        [Theory]
        [InlineData(2, 5)]
        public async Task IsConsistent_WhenDistributed(int numOfNodes, int numOfValues)
        {
            var cacheFactory = () =>
            {
                var moduleConfig = redisModuleConfig with { RedisSettings = redisModuleConfig.RedisSettings with { ResumeSession = false, ConsumerNameSuffix = Guid.NewGuid().ToString() } };
                return ComponentBuilder<IOrderedCache<int>>.Create()
                                                           .Bind(componentBuilder => componentBuilder.WithModules([new Baubit.Caching.Redis.DI.Module<int>(moduleConfig, [], [])]))
                                                           .Bind(componentBuilder => componentBuilder.WithFeatures([new Baubit.Logging.Features.F001()]))
                                                           .Bind(componentBuilder => componentBuilder.Build())
                                                           .Value;
            };

            var cts = new CancellationTokenSource();

            var cacheEnumPair = Enumerable.Range(0, numOfNodes)
                                          .Select(_ => cacheFactory())
                                          .ToDictionary(cache => cache, cache => cache.GetFutureAsyncEnumerator(cts.Token));

            for (int i = 0; i < numOfValues; i++)
            {
                Parallel.ForEach(cacheEnumPair, kvp => kvp.Key.Add(Random.Shared.Next(), out _));

                // There will be 1 value per node in all nodes
                // so move enumerator forward numOfNodes times
                for (int j = 0; j < numOfNodes; j++)
                {
                    var moveResult = await Task.WhenAll(cacheEnumPair.Select(async kvp => await kvp.Value.MoveNextAsync().ConfigureAwait(false)));

                    Assert.True(moveResult.All(r => r));

                    Assert.True(cacheEnumPair.Values.Select(e => e.Current.Id).Distinct().Count() == 1);
                    Assert.True(cacheEnumPair.Values.Select(e => e.Current.Value).Distinct().Count() == 1);
                }
            }

            Assert.True(cacheEnumPair.Keys.All(cache => cache.Count == numOfNodes * numOfValues));

            foreach (var cache in cacheEnumPair.Keys)
            {
                cache.Dispose();
            }

        }
    }
}
