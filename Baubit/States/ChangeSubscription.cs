namespace Baubit.States
{
    public class ChangeSubscription<T> : IDisposable where T : Enum
    {
        private IObserver<StateChanged<T>> _current;
        private IList<IObserver<StateChanged<T>>> _observers;
        public ChangeSubscription(IObserver<StateChanged<T>> current,
                                  IList<IObserver<StateChanged<T>>> observers)
        {
            _current = current;
            _observers = observers;
        }
        public void Dispose()
        {
            _observers.Remove(_current);
        }
    }
}
