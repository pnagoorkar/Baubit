using FluentResults;
using System.Collections.Specialized;

namespace Baubit.Caching
{
    public interface IPersistentCache<TValue> : IDisposable
    {
        public long Count { get; }
        Task<Result<long>> AddAsync(TValue value);
        Task<Result<TValue>> GetAsync(long id);
        Task<Result<TValue>> RemoveAsync(long id);
    }
}
