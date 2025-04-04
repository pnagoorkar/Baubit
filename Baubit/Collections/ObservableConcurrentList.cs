﻿using Baubit.IO.Channels;
using FluentResults;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using Baubit.Tasks;

namespace Baubit.Collections
{
    public class ObservableConcurrentList<T> : ConcurrentList<T>, IHostedService
    {
        public bool ObservationEnabled { get => _eventChannel != null; }
        public event Func<CollectionChangedEventArgs<T>, CancellationToken, Task> OnCollectionChangedAsync;

        private Channel<CollectionChangedEventArgs<T>> _eventChannel;

        private CancellationTokenSource instanceCancellationTokenSource;
        private Task deliveryTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (deliveryTask != null) return Task.CompletedTask;
            _eventChannel = Channel.CreateUnbounded<CollectionChangedEventArgs<T>>();
            instanceCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deliveryTask = Task.Run(() => _eventChannel.ReadAsync(DeliverNext, instanceCancellationTokenSource.Token));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            instanceCancellationTokenSource.Cancel();
            deliveryTask?.Wait(true);
            deliveryTask = null;
            _eventChannel?.FlushAndDispose();
            _eventChannel = null;
            return Task.CompletedTask;
        }

        private Task DeliverNext(CollectionChangedEventArgs<T> item, CancellationToken token)
        {
            return OnCollectionChangedAsync?.Invoke(item, token) ?? Task.CompletedTask;
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

        public override void RemoveAt(int index) => RemoveAtAndReturn(index);

        public new T RemoveAtAndReturn(int index)
        {
            var item = base.RemoveAtAndReturn(index);
            NotifyChange(this, [item], null, CollectionChangeType.Removed);
            return item;
        }

        private async Task<Result> NotifyChange(IList<T> sender, T[] oldItems, T[] newItems, CollectionChangeType collectionChangeType)
        {
            if (_eventChannel == null) throw new InvalidOperationException($"Did you forget to start observation ?{Environment.NewLine}{nameof(ObservableConcurrentList<T>.StartAsync)} has to be called explictly to enable observability on {typeof(ObservableConcurrentList<T>).AssemblyQualifiedName}");
            var changeEvent = new CollectionChangedEventArgs<T>(this, oldItems, newItems, collectionChangeType);
            return await _eventChannel.TryWriteWhenReadyAsync(changeEvent, Timeout.InfiniteTimeSpan, instanceCancellationTokenSource.Token);
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
