using FluentResults;
using FluentResults.Extensions;
using System.Runtime.CompilerServices;

namespace Baubit.Caching
{
    public static class CachingExtensions
    {
        public static async IAsyncEnumerable<Result<IEntry<TValue>>> ReadAsync<TValue>(this IOrderedCache<TValue> cache, long? startingId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (startingId != null)
            {
                var getResult = cache.Get(startingId.Value);
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
