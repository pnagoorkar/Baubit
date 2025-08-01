namespace Baubit.States
{
    public class StateChanged<T> : EventArgs where T : Enum
    {
        public T Current { get; init; }
        public T Previous { get; init; }
        public DateTime ChangedAtUTC { get; init; } = DateTime.UtcNow;
    }
}
