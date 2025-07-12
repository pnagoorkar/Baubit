using FluentResults;
using System.Collections.Specialized;

namespace Baubit.Caching
{
    public interface IPersistentCache<TValue> : IDisposable
    {
        Result<long> Count();
        Result<long> Add(TValue value);
        Result<TValue> Get(long id);
        Result<TValue> Remove(long id);
        Result Clear();
    }
}
