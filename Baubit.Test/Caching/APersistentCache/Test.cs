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
        public void CanInsertValues(int numOfItems)
        {
            var inMemoryCache = new InMemoryCache<int>();

            ConcurrentDictionary<long, int> insertedValues = new ConcurrentDictionary<long, int>();

            Parallel.For(0, numOfItems, i => inMemoryCache.Add(i).Bind(id => Result.Try(() => insertedValues.TryAdd(id, i))));

            Assert.Equal(numOfItems, inMemoryCache.Count().ValueOrDefault);

            var readResult = insertedValues.AsParallel()
                                           .Aggregate(Result.Ok(),
                                                      (seed, next) => seed.Bind(() => inMemoryCache.Get(next.Key))
                                                                          .Bind(val => Result.OkIf(next.Value == val, "Value mismatch at get!")));

            Assert.True(readResult.IsSuccess);

            var removeResult = insertedValues.AsParallel()
                                             .Aggregate(Result.Ok(),
                                                        (seed, next) => seed.Bind(() => inMemoryCache.Remove(next.Key))
                                                                            .Bind(val => Result.OkIf(next.Value == val, "Value mismatch at remove!")));

            Assert.True(removeResult.IsSuccess);
            Assert.Equal(0, inMemoryCache.Count().ValueOrDefault);
        }
    }
}
