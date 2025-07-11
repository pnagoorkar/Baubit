using Baubit.Test.Caching.Setup;
using FluentResults;
using FluentResults.Extensions;
using System.Collections.Concurrent;

namespace Baubit.Test.Caching.APersistentCache
{
    public class Test
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public async void CanInsertValues(int numOfItems)
        {
            var inMemoryCache = new InMemoryCache<int>();

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

            Parallel.For(0, numOfItems, async i => await inMemoryCache.AddAsync(i).Bind(id => Result.Try(() => insertedValues.TryAdd(id, i))));

            Assert.Equal(numOfItems, inMemoryCache.Count);

            var readResult = await insertedValues.AsParallel()
                                                 .Aggregate(Task.FromResult(Result.Ok()),
                                                            (seed, next) => seed.Bind(() => inMemoryCache.GetAsync(next.Key))
                                                                                .Bind(val => Result.OkIf(next.Value == val, "Value mismatch at get!")));

            Assert.True(readResult.IsSuccess);

            var removeResult = await insertedValues.AsParallel()
                                                   .Aggregate(Task.FromResult(Result.Ok()),
                                                              (seed, next) => seed.Bind(() => inMemoryCache.RemoveAsync(next.Key))
                                                                                  .Bind(val => Result.OkIf(next.Value == val, "Value mismatch at remove!")));

            Assert.True(removeResult.IsSuccess);
            Assert.Equal(0, inMemoryCache.Count);
        }
    }
}
