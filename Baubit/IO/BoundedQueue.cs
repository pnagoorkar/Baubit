namespace Baubit.IO
{
    public class BoundedQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly int _capacity;

        public event Action<T>? OnDequeue;

        public BoundedQueue(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }

            _capacity = capacity;
        }

        public void Enqueue(T item)
        {
            if (_queue.Count >= _capacity)
            {
                var dequeuedItem = _queue.Dequeue(); // Remove the oldest item
                OnDequeue?.Invoke(dequeuedItem);
            }

            _queue.Enqueue(item);
        }
    }
}
