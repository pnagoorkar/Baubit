using Baubit.Caching;
using Baubit.Caching.InMemory.DI;
using Baubit.Collections;
using Baubit.DI;
using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Baubit.Test.Caching.OrderedCache.InMemory
{
    public class Test
    {
        static IFeature[] inMemoryCacheFeatures =
        [
            new Baubit.Caching.InMemory.Features.F000<int>(),
            new Baubit.Logging.Features.F000()
        ];
        static IFeature[] inMemoryCacheWithAdaptiveResizingFeatures =
        [
            new Baubit.Caching.InMemory.Features.F001<int>(),
            new Baubit.Logging.Features.F001()
        ];

        [Fact]
        public async Task CanAwaitValues()
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var nextEntryTask = inMemoryCache.GetNextAsync();

            var addResult = inMemoryCache.Add(Random.Shared.Next(0, 10), out var entry);
            Assert.True(addResult);

            var nextEntry = await nextEntryTask;

            Assert.Equal(entry.Value, nextEntry.Value);
        }

        [Fact]
        public async Task CanCancelGetNextAsync()
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource = new CancellationTokenSource();

            var asyncGetter = inMemoryCache.GetNextAsync(null, cancellationTokenSource.Token);

            await Task.Delay(500).ContinueWith(_ => cancellationTokenSource.Cancel());

            await Assert.ThrowsAsync<TaskCanceledException>(() => asyncGetter);
        }

        [Fact]
        public async Task ReaderCancellationsAreIsolated()
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource1 = new CancellationTokenSource();
            var cancellationTokenSource2 = new CancellationTokenSource();

            var res2 = inMemoryCache.GetNextAsync(null, cancellationTokenSource2.Token);

            var cancellationRunner = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                cancellationTokenSource1.Cancel();
                inMemoryCache.Add(1, out _);
            });

            await Assert.ThrowsAsync<TaskCanceledException>(() => inMemoryCache.GetNextAsync(null, cancellationTokenSource1.Token));

            var entry = await res2;
            Assert.NotNull(entry);
            Assert.Equal(1, entry.Value);
        }

        [Theory]
        [InlineData(1000)]
        public async Task UsesL1CacheForFastLookup(int numOfItems)
        {
            var cacheWithDummyL2 = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                       .Bind(componentBuilder => componentBuilder.WithModules(new Setup.DummyL2.DI.Module<int>(new Setup.DummyL2.DI.Configuration { IncludeL1Caching = true, L1MaxCap = numOfItems, L1MinCap = numOfItems }, [], [])))
                                                                       .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F000()))
                                                                       .Bind(componentBuilder => componentBuilder.Build()).Value;

            ConcurrentDictionary<Guid, int> insertedValues = new ConcurrentDictionary<Guid, int>();

            var parallelLoopResult = Parallel.For(0, numOfItems, i =>
            {
                if (!(cacheWithDummyL2.Add(i, out var entry) && insertedValues.TryAdd(entry.Id, i)))
                {
                    throw new Exception("Insert failed!");
                }
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);

            Assert.Equal(numOfItems, cacheWithDummyL2.Count);

            var parallelLoopResultRead = Parallel.ForEach(insertedValues, kvp =>
            {
                if (!(cacheWithDummyL2.GetEntryOrDefault(kvp.Key, out var entry) && kvp.Value == entry.Value))
                {
                    throw new Exception("Value mismatch at get!");
                }
            });

            Assert.Null(parallelLoopResultRead.LowestBreakIteration);

            Assert.Equal(numOfItems, cacheWithDummyL2.Count);
            var currentCount = cacheWithDummyL2.Count;

            var parallelLoopResultRemove = Parallel.ForEach(insertedValues.ToArray(), kvp =>
            {
                if(!cacheWithDummyL2.Remove(kvp.Key, out var entry))
                {
                    throw new Exception("Remove failed!");
                }
            });

            Assert.Equal(0, cacheWithDummyL2.Count);
        }

        [Theory]
        [InlineData(1000, 100, 5, 20)]
        public async Task CanReadAndWriteSimultaneously(int numOfItems, int numOfReaders, int writerBatchMinSize, int writerBatchMaxSize)
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheWithAdaptiveResizingFeatures))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

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
            var readerBurst = () => { Parallel.For(0, numOfReaders, i => readerTasks.Add(reader(inMemoryCache.GetAsyncEnumerator(readCTS.Token)))); };
            var writerBurst = (int batchSize) => Parallel.For(0, batchSize, i => inMemoryCache.Add(i, out _));

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

        [Theory]
        [InlineData(1)]
        public async Task CanEvictAfterEveryX(int x)
        {
            var configuration = Baubit.Caching.InMemory.DI.Configuration.C001 with { CacheConfiguration = new Baubit.Caching.Configuration { EvictAfterEveryX = x } };
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                    .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F000()))
                                                                    .Bind(componentBuilder => componentBuilder.WithModules(new Module<int>(configuration, [], [])))
                                                                    .Bind(componentBuilder => componentBuilder.Build()).Value;

            var enumerator = inMemoryCache.GetAsyncEnumerator();

            inMemoryCache.Add(Random.Shared.Next(), out var entry);
            Assert.Equal(inMemoryCache.Count, 1);

            Assert.Null(enumerator.Current);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

            inMemoryCache.Add(Random.Shared.Next(), out entry);
            Assert.Equal(inMemoryCache.Count, 1);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

            inMemoryCache.Add(Random.Shared.Next(), out entry);
            Assert.Equal(inMemoryCache.Count, 1);

            await enumerator.MoveNextAsync();
            Assert.Equal(entry.Value, enumerator.Current.Value);

        }
    }

    public class ReadTracker
    {
        public long Id { get; init; }
        public int ReadCount { get => _readCount; }

        private int _readCount = 0;

        public int Increment()
        {
            return Interlocked.Increment(ref _readCount);
        }
    }
}
