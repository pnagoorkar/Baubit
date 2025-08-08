using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.DI;
using Baubit.Tasks;
using Baubit.Test.Caching.Setup;
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
        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        public async Task CanReadAndWriteSimultaneously(int numOfItems)
        {
            var inMemoryCache = ComponentBuilder<OrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();
            ConcurrentDictionary<long, int> readValues = new ConcurrentDictionary<long, int>();

            CancellationTokenSource readCTS = new CancellationTokenSource();

            var read = async () =>
            {
                try
                {
                    await foreach (var item in inMemoryCache.ReadAsync(null, readCTS.Token))
                    {
                        item.Bind(entry => Result.Try(() => readValues.TryAdd(entry.Id, entry.Value))).ThrowIfFailed();
                        if (readValues.Count == numOfItems) break;
                    }
                    return Result.Ok();
                }
                catch (TaskCanceledException)
                {
                    return Result.Ok();
                }
                catch (Exception exp)
                {
                    return Result.Fail(new ExceptionalError(exp));
                }
            };

            var reader = read();

            Parallel.For(0, numOfItems, i => inMemoryCache.Add(i).Bind(entry => Result.Try(() => insertedValues.TryAdd(entry.Id, i))));

            Assert.Equal(numOfItems, inMemoryCache.Count().ValueOrDefault);

            await reader;

            Assert.Equal(insertedValues, readValues);

            var removeResult = insertedValues.AsParallel()
                                             .Aggregate(Result.Ok(),
                                                        (seed, next) => seed.Bind(() => inMemoryCache.Remove(next.Key))
                                                                            .Bind(entry => Result.OkIf(next.Value == entry.Value, "Value mismatch at remove!")));

            Assert.True(removeResult.IsSuccess);
            Assert.Equal(0, inMemoryCache.Count().ValueOrDefault);
        }

        [Fact]
        public async Task CanAwaitValues()
        {
            var inMemoryCache = ComponentBuilder<OrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

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
            var inMemoryCache = ComponentBuilder<OrderedCache<int>>.Create().Bind(componentBuilder => componentBuilder.WithFeatures(inMemoryCacheFeatures)).Bind(componentBuilder => componentBuilder.Build()).Value;

            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () => { await Task.Delay(500); cancellationTokenSource.Cancel(); });            

            var getResult = await inMemoryCache.GetNextAsync(null, cancellationTokenSource.Token);

            Assert.True(getResult.IsFailed);
            Assert.Contains(getResult.Errors, err => err is ExceptionalError expErr && expErr.Message == "A task was canceled.");
        }

        [Theory]
        [InlineData(1000)]
        public async Task UsesL1CacheForFastLookup(int numOfItems)
        {
            var dummyCache = ComponentBuilder<DummyCache<int>>.Create()
                                                              .Bind(componentBuilder => componentBuilder.WithModules(new Setup.DI.Module<int>(new Setup.DI.Configuration { CacheConfiguration = new Baubit.Caching.Configuration() { L1StoreCap = numOfItems } }, [], [])))
                                                              .Bind(componentBuilder => componentBuilder.WithFeatures(new Baubit.Logging.Features.F000()))
                                                              .Bind(componentBuilder => componentBuilder.Build()).Value;

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

            Parallel.For(0, numOfItems, i => dummyCache.Add(i).Bind(entry => Result.Try(() => insertedValues.TryAdd(entry.Id, i))));

            Assert.Equal(numOfItems, dummyCache.L1StoreCount);

            var readResult = insertedValues.AsParallel()
                                           .Aggregate(Result.Ok(),
                                                      (seed, next) => seed.Bind(() => dummyCache.Get(next.Key))
                                                                          .Bind(entry => Result.OkIf(next.Value == entry.Value, "Value mismatch at get!")));

            Assert.True(readResult.IsSuccess);

            Assert.Equal(numOfItems, dummyCache.L1StoreCount);
            var currentCount = dummyCache.L1StoreCount;

            var removeResult = insertedValues.AsParallel()
                                             .Aggregate(Result.Ok(),
                                                        (seed, next) => seed.Bind(() => dummyCache.Remove(next.Key))
                                                                            .Bind(entry => Result.OkIf(--currentCount == dummyCache.L1StoreCount, "Count does not tally after remove!")));

            Assert.True(removeResult.IsSuccess);
            Assert.Equal(0, dummyCache.L1StoreCount);
        }
    }
}
