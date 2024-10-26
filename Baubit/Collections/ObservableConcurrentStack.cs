using System.Collections;

namespace Baubit.Collections
{
    public class ObservableConcurrentStack<T> : IReadOnlyCollection<T>
    {
        public event Action<CollectionChangedEventArgs<T>> OnCollectionChanged
        {
            add => _list.OnCollectionChanged += value;
            remove => _list.OnCollectionChanged -= value;
        }
        private ObservableConcurrentList<T> _list = new ObservableConcurrentList<T>();

        public int Count => _list.Count;

        public void Push(T item) => _list.Add(item);

        public void Pop() => _list.RemoveAt(0);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
