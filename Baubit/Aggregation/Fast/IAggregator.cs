using Baubit.Observation;

namespace Baubit.Aggregation.Fast
{
    public interface IAggregator<T> : IPublisher<T>, IDisposable
    {
        public bool CanPublish { get; }
        bool Publish(T item);
    }
}
