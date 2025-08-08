using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.DI;
using Baubit.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Baubit.Test.Caching.AOrderedCache
{
    public class Test
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        //[InlineData(100000)]
        //[InlineData(1000000)]
        public void CanInsertValues(int numOfItems)
        {
            var loggerFactory = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var inMemoryCache = new OrderedCache<int>(loggerFactory);

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

            Parallel.For(0, numOfItems, i => inMemoryCache.Add(i).Bind(entry => Result.Try(() => insertedValues.TryAdd(entry.Id, i))));

            Assert.Equal(numOfItems, inMemoryCache.Count().ValueOrDefault);

            var readResult = insertedValues.AsParallel()
                                           .Aggregate(Result.Ok(),
                                                      (seed, next) => seed.Bind(() => inMemoryCache.Get(next.Key))
                                                                          .Bind(entry => Result.OkIf(next.Value == entry.Value, "Value mismatch at get!")));

            Assert.True(readResult.IsSuccess);

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
            var loggerFactory = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var inMemoryCache = new OrderedCache<int>(loggerFactory);
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
            var loggerFactory = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var inMemoryCache = new OrderedCache<int>(loggerFactory);
            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () => { await Task.Delay(500); cancellationTokenSource.Cancel(); });            

            var getResult = await inMemoryCache.GetNextAsync(null, cancellationTokenSource.Token);

            Assert.True(getResult.IsFailed);
            Assert.Contains(getResult.Errors, err => err is ExceptionalError expErr && expErr.Message == "A task was canceled.");
        }
    }
}
