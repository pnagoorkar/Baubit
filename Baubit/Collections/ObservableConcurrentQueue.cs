using Microsoft.Extensions.Hosting;
using System.Collections;

namespace Baubit.Collections
{
    public class ObservableConcurrentQueue<T> : IReadOnlyCollection<T>, IHostedService
    {
        public event Func<CollectionChangedEventArgs<T>, CancellationToken, Task> OnCollectionChangedAsync
        {
            add => _list.OnCollectionChangedAsync += value;
            remove => _list.OnCollectionChangedAsync -= value;
        }
        private ObservableConcurrentList<T> _list = new ObservableConcurrentList<T>();
        public bool ObservationEnabled { get => _list.ObservationEnabled; }

        public int Count => _list.Count;

        public void Enqueue(T item) => _list.Add(item);

        public bool TryDequeue(out T item) => _list.Remove(items => items.First(), out item);

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
