using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.Collections;
using Baubit.DI;
using Baubit.Tasks;
using Baubit.Test.States.State.Setup;
using Baubit.Traceability;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Baubit.Test.Caching.AOrderedCache
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

            Result<IEntry<int>> getNextResult = null;
            var autoResetEvent = new AutoResetEvent(false);

            Parallel.Invoke(async () =>
            {
                getNextResult = await inMemoryCache.GetNextAsync();
                autoResetEvent.Set();
            });

            await Task.Delay(100); //to ensure the GetNextAsync resets internal awaiter before the Add sets it
            Assert.Null(getNextResult);

            var addResult = inMemoryCache.Add(Random.Shared.Next(0, 10));
            Assert.True(addResult.IsSuccess);

            autoResetEvent.WaitOne();

            Assert.True(getNextResult.IsSuccess);
            Assert.Equal(addResult.Value.Value, getNextResult.Value.Value);

        }

        [Fact]
        public async Task CanCancelGetNextAsync()
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () => { await Task.Delay(500); cancellationTokenSource.Cancel(); });            

            var getResult = await inMemoryCache.GetNextAsync(null, cancellationTokenSource.Token);

            Assert.True(getResult.IsFailed);
            Assert.Contains(getResult.Errors, err => err is ExceptionalError expErr && expErr.Message == "A task was canceled.");
        }

        //[Theory]
        //[InlineData(1000)]
        //public async Task UsesL1CacheForFastLookup(int numOfItems)
        //{
        //    var dummyCache = (DummyCache<int>)ComponentBuilder<IOrderedCache<int>>.Create()
        //                                                                          .Bind(componentBuilder => componentBuilder.WithModules(new Setup.DI.Module<int>(new Setup.DI.Configuration { CacheConfiguration = new Baubit.Caching.Configuration() { L1StoreInitialCap = numOfItems } }, [], [])))
        //                                                                          .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F000()))
        //                                                                          .Bind(componentBuilder => componentBuilder.Build()).Value;

        //    ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

        //    Parallel.For(0, numOfItems, i => dummyCache.Add(i).Bind(entry => Result.Try(() => insertedValues.TryAdd(entry.Id, i))));

        //    Assert.Equal(numOfItems, dummyCache.L1StoreCount);

        //    var readResult = insertedValues.AsParallel()
        //                                   .Aggregate(Result.Ok(),
        //                                              (seed, next) => seed.Bind(() => dummyCache.GetEntryOrDefault(next.Key))
        //                                                                  .Bind(entry => Result.OkIf(next.Value == entry.Value, "Value mismatch at get!")));

        //    Assert.True(readResult.IsSuccess);

        //    Assert.Equal(numOfItems, dummyCache.L1StoreCount);
        //    var currentCount = dummyCache.L1StoreCount;

        //    var removeResult = insertedValues.AsParallel()
        //                                     .Aggregate(Result.Ok(),
        //                                                (seed, next) => seed.Bind(() => dummyCache.Remove(next.Key))
        //                                                                    .Bind(entry => Result.OkIf(--currentCount == dummyCache.L1StoreCount, "Count does not tally after remove!")));

        //    Assert.True(removeResult.IsSuccess);
        //    Assert.Equal(0, dummyCache.L1StoreCount);
        //}

        [Theory]
        [InlineData(1000, 100)]
        public async Task CanReadAndWriteSimultaneously(int numOfItems, int numOfReaders)
        {
            var inMemoryCache = ComponentBuilder<IOrderedCache<int>>.Create()
                                                                   .Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheWithAdaptiveResizingFeatures))
                                                                   .Bind(componentBuilder => componentBuilder.Build()).Value;

            ConcurrentDictionary<long, int> readMap = new ConcurrentDictionary<long, int>();

            long numRead = 0;
            int expectedNumOfReads = numOfItems * numOfReaders;
            CancellationTokenSource readCTS = new CancellationTokenSource();
            SemaphoreSlim readSyncer = new SemaphoreSlim(1);

            ConcurrentList<Task<Result>> readerTasks = new ConcurrentList<Task<Result>>();

            ConcurrentDictionary<long, double> deliveryTimes = new ConcurrentDictionary<long, double>();

            var reader = async (int i) =>
            {
                return await inMemoryCache.ReadAllAsync(null, readCTS.Token)
                                          .AggregateAsync(entry =>
                                          {
                                              readSyncer.Wait();
                                              Result.Try(() =>
                                              {
                                                  if (!readMap.ContainsKey(entry.Id)) readMap.TryAdd(entry.Id, 0);
                                                  readMap[entry.Id]++;
                                                  if (readMap[entry.Id] == numOfReaders)
                                                  {
                                                      deliveryTimes.TryAdd(entry.Id, DateTime.UtcNow.Subtract(entry.CreatedOnUTC).TotalMilliseconds);
                                                  }
                                                  if (++numRead == expectedNumOfReads)
                                                  {
                                                      readCTS.Cancel();
                                                  }
                                              });
                                              readSyncer.Release();
                                              return Result.Ok();
                                          }, readCTS.Token);
            };

            var readerBurst = () => { Parallel.For(0, numOfReaders, i => readerTasks.Add(reader(i))); };

            var writerBurst = (int batchSize) => Parallel.For(0, batchSize, i => inMemoryCache.Add(i));

            var insertedCount = 0;
            var deletedItems = new HashSet<long>();
            var deleterBurst = async () =>
            {
                await Task.Yield();
                while (deletedItems.Count < numOfItems)
                {
                    readMap.Where(kvp => !deletedItems.Contains(kvp.Key) && kvp.Value == numOfReaders)
                           .Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => inMemoryCache.Remove(next.Key)).Bind(_ => Result.Try(() => { deletedItems.Add(next.Key); })));
                    Thread.Sleep(100);
                }
            };

            readerBurst();
            var deleter = deleterBurst();

            while (insertedCount < numOfItems)
            {
                var batchSize = Math.Min(numOfItems - insertedCount, Random.Shared.Next(5, 20));
                writerBurst(batchSize);
                Thread.Sleep(10);
                insertedCount += batchSize;
            }

            await Task.WhenAll(readerTasks);
            await deleter;

            var fastestDeliveryTime = deliveryTimes.Values.Min();
            var slowestDeliveryTime = deliveryTimes.Values.Max();
            var avgDeliveryTime = deliveryTimes.Values.Average();
            
        }
    }
}
