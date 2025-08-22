using Baubit.Observation;
using FluentResults;

namespace Baubit.Aggregation
{
    public interface IAggregator<T> : IPublisher<T>, IDisposable
    {
        Result Publish(T item);
    }
}
