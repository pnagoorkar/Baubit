using FluentResults;
using FluentResults.Extensions;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    public static class CachingExtensions
    {
        public static Result<TValue> GetValue<TValue>(this IOrderedCache<TValue> cache, long id)
        {
            return cache.GetEntryOrDefault(id).Bind(entry => Result.Try(() => entry.Value));
        }
        public static IEnumerable<TValue> EnumerateValues<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            return cache.EnumerateEntries(startingId).Select(entry => entry.Value);
        }
        public static IEnumerable<IEntry<TValue>> EnumerateEntries<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            return cache.ReadAll(startingId).Select(result => result.Value);
        }
        public static IEnumerable<Result<IEntry<TValue>>> ReadAll<TValue>(this IOrderedCache<TValue> cache, long? startingId = null)
        {
            var getResult = default(Result<IEntry<TValue>>);
            if (startingId != null)
            {
                getResult = cache.GetEntryOrDefault(startingId.Value);
                if (getResult.IsSuccess && getResult.Value != null)
                {
                    yield return getResult;
                }
            }
            long? afterId = null;
            do
            {
                afterId = getResult?.Value?.Id;
                getResult = cache.GetNextOrDefault(afterId);
                if (getResult == default(Result<IEntry<TValue>>)) break;
                yield return getResult;
            } while (getResult != default(Result<IEntry<TValue>>));

        }

        public static async IAsyncEnumerable<TValue> EnumerateValuesAsync<TValue>(this IOrderedCache<TValue> cache, long? 
                                                                                  startingId = null, 
                                                                                  [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entry in cache.EnumerateEntriesAsync(startingId, cancellationToken))
            {
                yield return entry.Value;
            }
        }

        public static async IAsyncEnumerable<IEntry<TValue>> EnumerateEntriesAsync<TValue>(this IOrderedCache<TValue> cache, 
                                                                                           long? startingId = null, 
                                                                                           [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in cache.ReadAllAsync(startingId, cancellationToken))
            {
                yield return result.Value;
            }
        }

        public static async IAsyncEnumerable<Result<IEntry<TValue>>> ReadAllAsync<TValue>(this IOrderedCache<TValue> cache, 
                                                                                          long? startingId = null, 
                                                                                          [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (startingId != null)
            {
                var getResult = cache.GetEntryOrDefault(startingId.Value);
                if (getResult.IsSuccess && getResult.Value != null)
                {
                    yield return getResult;
                }
            }
            long? afterId = startingId;
            do
            {
                yield return await cache.GetNextAsync(afterId, cancellationToken).Bind(entry => Result.Try(() => { afterId = entry.Id; return entry; }));

            } while (!cancellationToken.IsCancellationRequested);
        }

        public static async Task<Result> AggregateAsync<T>(this IAsyncEnumerable<T> asyncEnumerable,
                                                           Func<T, Result> func,
                                                           CancellationToken cancellationToken = default)
        {
            var retVal = Result.Ok();
            await foreach (var item in asyncEnumerable)
            {
                retVal = retVal?.Bind(() => func(item));
                if (cancellationToken.IsCancellationRequested || retVal.IsFailed) break;
            }
            return retVal;
        }

        public static async Task<Result> AggregateAsync<T>(this IAsyncEnumerable<Result<T>> asyncEnumerable,
                                                           Func<T, Result> func,
                                                           CancellationToken cancellationToken = default)
        {
            var retVal = Result.Ok();
            await foreach (var item in asyncEnumerable)
            {
                retVal = retVal?.Bind(() => item.Bind(entry => func(entry)));
                if (cancellationToken.IsCancellationRequested || retVal.IsFailed) break;
            }
            return retVal;
        }
    }
}
