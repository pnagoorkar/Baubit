namespace Baubit.IO
{
    public class KMPFrame
    {
        public string Value { get; init; }

        private int currentIndex = 0;
        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
            private set
            {
                currentIndex = value;
            }
        }
        public bool ReachedTheEnd { get => CurrentIndex >= Value.Length; }

        public bool CanMoveForward { get => !ReachedTheEnd; }

        private char CurrentValue { get => Value[CurrentIndex]; }

        public int[] LPS { get; init; }

        public KMPFrame(string value)
        {
            Value = value;
            LPS = BuildLPSArray(Value);
        }

        public void MoveNext(char input)
        {
            if (ReachedTheEnd) return;

            while (CurrentIndex > 0 && CurrentValue != input) CurrentIndex = LPS[CurrentIndex - 1];

            if (CurrentValue == input) CurrentIndex++;
        }

        public void Reset()
        {
            CurrentIndex = 0;
        }

        private static int[] BuildLPSArray(string pattern)
        {
            int length = 0;
            int[] lps = new int[pattern.Length];
            lps[0] = 0;
            int i = 1;

            while (i < pattern.Length)
            {
                if (pattern[i] == pattern[length])
                {
                    length++;
                    lps[i] = length;
                    i++;
                }
                else
                {
                    if (length != 0)
                    {
                        length = lps[length - 1];
                    }
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }

            return lps;
        }
    }
}
