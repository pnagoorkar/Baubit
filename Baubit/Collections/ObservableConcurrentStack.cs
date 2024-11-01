using Microsoft.Extensions.Hosting;
using System.Collections;

namespace Baubit.Collections
{
    public class ObservableConcurrentStack<T> : IReadOnlyCollection<T>, IHostedService
    {
        public event Func<CollectionChangedEventArgs<T>, CancellationToken, Task> OnCollectionChangedAsync
        {
            add => _list.OnCollectionChangedAsync += value;
            remove => _list.OnCollectionChangedAsync -= value;
        }
        private ObservableConcurrentList<T> _list = new ObservableConcurrentList<T>();
        public bool ObservationEnabled { get => _list.ObservationEnabled; }

        public int Count => _list.Count;

        public void Push(T item) => _list.Add(item);

        public T Pop() => _list.RemoveAtAndReturn(0);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _list.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _list.StopAsync(cancellationToken);
        }
    }
}
