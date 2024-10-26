using Baubit.IO;
using System.Threading.Channels;

namespace Baubit.Collections
{
    public class ObservableConcurrentList<T> : ConcurrentList<T>
    {
        public event Action<CollectionChangedEventArgs<T>> OnCollectionChanged;

        private Channel<CollectionChangedEventArgs<T>> _eventChannel = Channel.CreateUnbounded<CollectionChangedEventArgs<T>>();

        private CancellationTokenSource instanceCancellationTokenSource = new CancellationTokenSource();
        public ObservableConcurrentList()
        {
            Task.Run(() => DeliverEvents(instanceCancellationTokenSource.Token));
        }

        public override T this[int index]
        {
            get => base[index];
            set
            {
                base[index] = value;
                NotifyChange(this, null, [base[index]], CollectionChangeType.Added);
            }
        }

        public override void Add(T item)
        {
            base.Add(item);
            NotifyChange(this, null, [item], CollectionChangeType.Added);
        }

        public override void Clear()
        {
            T[] array = base.RemoveAndReturnAll();
            NotifyChange(this, array, null, CollectionChangeType.Removed);
        }

        public override void Insert(int index, T item)
        {
            base.Insert(index, item);
            NotifyChange(this, null, [item], CollectionChangeType.Added);
        }

        public override bool Remove(T item)
        {
            var removeResult = base.Remove(item);
            NotifyChange(this, [item], null, CollectionChangeType.Removed);
            return removeResult;
        }

        public override void RemoveAt(int index)
        {
            var item = base.RemoveAtAndReturn(index);
            NotifyChange(this, [item], null, CollectionChangeType.Removed);
        }

        private async void NotifyChange(IList<T> sender, T[] oldItems, T[] newItems, CollectionChangeType collectionChangeType)
        {
            var changeEvent = new CollectionChangedEventArgs<T>(this, oldItems, newItems, collectionChangeType);
            await _eventChannel.TryWriteWhenReadyAsync(changeEvent, instanceCancellationTokenSource.Token);
        }

        private async Task DeliverEvents(CancellationToken cancellationToken = default)
        {
            await foreach (var @event in _eventChannel.EnumerateAsync(cancellationToken))
            {
                OnCollectionChanged?.Invoke(@event);
            }
        }

        ~ObservableConcurrentList()
        {
            instanceCancellationTokenSource.Cancel();
        }
    }
    public enum CollectionChangeType
    {
        Added,
        Removed,
        Modified
    }
    public class CollectionChangedEventArgs<T>
    {
        public IList<T> Sender { get; init; }
        public T[] OldItems { get; init; }
        public T[] NewItems { get; init; }
        public CollectionChangeType ChangeType { get; init; }
        public CollectionChangedEventArgs(IList<T> sender, T[] oldItems, T[] newItems, CollectionChangeType collectionChangeType)
        {
            Sender = sender;
            OldItems = oldItems;
            NewItems = newItems;
            ChangeType = collectionChangeType;
        }
    }
}
