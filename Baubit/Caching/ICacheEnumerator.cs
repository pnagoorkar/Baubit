namespace Baubit.Caching
{
    public interface ICacheEnumerator
    {
        public Guid? CurrentId { get; }
    }

    public interface IFutureAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        IAsyncEnumerator<T> GetFutureAsyncEnumerator(CancellationToken cancellationToken = default);
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetFutureAsyncEnumerator(cancellationToken);
    }
}
