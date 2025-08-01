using Baubit.Observation;

namespace Baubit.States
{
    public class ChangeSubscription<T> : IDisposable where T : Enum
    {
        private ISubscriber<StateChanged<T>> _current;
        private IList<ISubscriber<StateChanged<T>>> _subscribers;
        public ChangeSubscription(ISubscriber<StateChanged<T>> current,
                                  IList<ISubscriber<StateChanged<T>>> subscribers)
        {
            _current = current;
            _subscribers = subscribers;
        }
        public void Dispose()
        {
            _subscribers.Remove(_current);
        }
    }
}
