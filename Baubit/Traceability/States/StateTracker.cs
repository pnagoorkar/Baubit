using FluentResults;

namespace Baubit.Traceability.States
{
    public sealed class StateTracker<T> where T : struct, Enum
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private StateNode<T> _current;

        public StateTracker(T initial, IReason?[] reasons = null) => _current = new StateNode<T>(initial, reasons, previous: null);

        public Result<StateNode<T>> TryAdvance(T newValue, 
                                               IReason?[] reasons = null,
                                               CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return Result.Fail("Operation cancelled.");

            _lock.EnterWriteLock();
            try
            {
                return Result.FailIf(_current.Next is not null, "A transition has already occurred.")
                             .Bind(() => Result.Try(() => new StateNode<T>(newValue, reasons, _current)))
                             .Bind(next => Result.Try(() => _current.LinkNext(next)).Bind(() => Result.Try(() => _current = next)))
                             .Bind(_ => Result.Ok(_current));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public StateNode<T> Current
        {
            get
            {
                _lock.EnterReadLock();
                try { return _current; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public override string ToString() => Current.Value.ToString();
    }
    public sealed class StateNode<T> where T : struct, Enum
    {
        public T Value { get; }
        public IReason?[] Reasons { get; }
        public StateNode<T>? Previous { get; }
        public StateNode<T>? Next { get; private set; }
        public DateTime CreatedAtUTC { get; init; } = DateTime.UtcNow;

        internal StateNode(T value, IReason?[] reasons, StateNode<T>? previous)
        {
            Value = value;
            Reasons = reasons;
            Previous = previous;
        }

        internal void LinkNext(StateNode<T> next) => Next = next;
    }
}
