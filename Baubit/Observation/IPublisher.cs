using FluentResults;

namespace Baubit.Observation
{
    public interface IPublisher<T>
    {
        public Task<bool> SubscribeAsync(ISubscriber<T> subscriber, CancellationToken cancellationToken = default);
    }
}
