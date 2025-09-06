using Baubit.Caching;
using Baubit.Collections;
using Baubit.DI;
using System.Collections.Concurrent;

namespace Baubit.Test.Caching.OrderedCache
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

            IEntry<int> nextEntry = null;
            var autoResetEvent = new AutoResetEvent(false);

            Parallel.Invoke(async () =>
            {
                nextEntry = await inMemoryCache.GetNextAsync().ConfigureAwait(false);
                autoResetEvent.Set();
            });

            await Task.Delay(100); //to ensure the GetNextAsync resets internal awaiter before the Add sets it
            Assert.Null(nextEntry);

            var addResult = inMemoryCache.Add(Random.Shared.Next(0, 10), out var entry);
            Assert.True(addResult);

            autoResetEvent.WaitOne();
            Assert.Equal(entry.Value, nextEntry.Value);

        }

        [Fact]
        public async Task CanCancelGetNextAsync()
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource = new CancellationTokenSource();

            var cancellationRunner = Task.Run(async () => { await Task.Delay(500).ConfigureAwait(false); cancellationTokenSource.Cancel(); });

            var task = Task.Run(async () => await inMemoryCache.GetNextAsync(null, cancellationTokenSource.Token));

            await cancellationRunner;

            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
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

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

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

            var readMap = new ConcurrentDictionary<long, ReadTracker>(Enumerable.Range(1, numOfItems).ToDictionary(i => (long)i, i => new ReadTracker { Id = i }));

            long numRead = 0;
            int expectedNumOfReads = numOfItems * numOfReaders;
            CancellationTokenSource readCTS = new CancellationTokenSource();
            SemaphoreSlim readSyncer = new SemaphoreSlim(1);

            ConcurrentList<Task<bool>> readerTasks = new ConcurrentList<Task<bool>>();

            var reader = async (int i) =>
            {
                try
                {
                    await inMemoryCache.EnumerateEntriesAsync(null, readCTS.Token)
                                       .AggregateAsync(entry =>
                                       {
                                           try
                                           {
                                               readMap[entry.Id].Increment();
                                               if (Interlocked.Increment(ref numRead) == expectedNumOfReads)
                                               {
                                                   readCTS.Cancel();
                                               }
                                               return true;
                                           }
                                           catch (Exception exp)
                                           {
                                               return false;
                                           }
                                       }, readCTS.Token).ConfigureAwait(false);
                }
                catch(Exception exp)
                {
                    return false;
                }
                return true;
            };

            var readerBurst = () => { Parallel.For(0, numOfReaders, i => readerTasks.Add(reader(i))); };

            var writerBurst = (int batchSize) => Parallel.For(0, batchSize, i => inMemoryCache.Add(i, out _));

            var insertedCount = 0;
            var deletedItems = new HashSet<long>();
            var deleterBurst = async () =>
            {
                await Task.Yield();
                while (deletedItems.Count < numOfItems)
                {
                    readMap.Where(kvp => !deletedItems.Contains(kvp.Key) && kvp.Value.ReadCount == numOfReaders)
                           .Aggregate(true, (seed, next) => seed &&
                                                            inMemoryCache.Remove(next.Key, out var deletedEntry) &&
                                                            deletedItems.Add(deletedEntry.Id));
                    await Task.Delay(100).ConfigureAwait(false);
                }
            };

            readerBurst();
            var deleter = deleterBurst();

            while (insertedCount < numOfItems)
            {
                var batchSize = Math.Min(numOfItems - insertedCount, Random.Shared.Next(writerBatchMinSize, writerBatchMaxSize));
                writerBurst(batchSize);
                Thread.Sleep(10);
                insertedCount += batchSize;
            }

            await Task.WhenAll(readerTasks);
            await deleter;
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
