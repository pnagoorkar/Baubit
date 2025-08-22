using FluentResults;

namespace Baubit.Observation
{
    public interface IPublisher<T>
    {
        public Result<IDisposable> Subscribe(ISubscriber<T> subscriber);
    }
}
