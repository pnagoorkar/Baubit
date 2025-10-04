using FluentResults;

namespace Baubit.Observation
{
    public interface IPublisher<T>
    {
        public Task<bool> SubscribeAsync<TItem>(ISubscriber<TItem> subscriber, CancellationToken cancellationToken = default) where TItem : T;
    }
}
