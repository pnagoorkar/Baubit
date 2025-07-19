using Baubit.IO.Channels;
using FluentResults;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Baubit.Caching
{
    public interface IOrderedCache<TValue> : IDisposable
    {
        Result<long> Count();
        Result<IEntry<TValue>> Add(TValue value);
        Result<IEntry<TValue>> Get(long id);
        Result<IEntry<TValue>> Remove(long id);
        Result Clear();
    }

    public static class PersistentCacheExtensions
    {
        public static async IAsyncEnumerable<TValue> ReadAllAsync<TValue>(this IOrderedCache<TValue> persistentCache, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Channel<TValue> channel = Channel.CreateBounded<TValue>(new BoundedChannelOptions(1));
            await foreach(var next in channel.EnumerateAsync(cancellationToken))
            {
                yield return next;
            }
        }
    }
}
