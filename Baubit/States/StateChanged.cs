namespace Baubit.States
{
    public class StateChanged<T> : EventArgs where T : Enum
    {
        public int Current { get; init; }
        public int Previous { get; init; }
    }
}
