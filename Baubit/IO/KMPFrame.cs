﻿using System.Text;

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
                if (currentIndex == Value.Length)
                {
                    ReachedTheEnd = true;
                }
            }
        }
        public bool ReachedTheEnd { get; private set; }

        private char CurrentValue { get => Value[CurrentIndex]; }

        public int[] LPS { get; init; }

        BoundedQueue<char> cache;
        StringBuilder cacheOverflow = new StringBuilder();
        private bool cachingEnabled = false;

        public KMPFrame(string value)
        {
            Value = value;
            LPS = BuildLPSArray(Value);
            cache = new BoundedQueue<char>(Value.Length);
            cache.OnOverflow += @char => cacheOverflow.Append(@char);
        }

        public void MoveNext(char input)
        {
            if (ReachedTheEnd) return;

            while (CurrentIndex > 0 && CurrentValue != input) CurrentIndex = LPS[CurrentIndex - 1];

            if (CurrentValue == input) CurrentIndex++;

            if (cachingEnabled) cache.Enqueue(input);
        }

        public void BeginCaching()
        {
            cachingEnabled = true;
        }

        private void EndCaching()
        {
            cachingEnabled = false;
        }

        public string GetOverflowedCache()
        {
            return cacheOverflow.ToString();
        }

        public void Reset()
        {
            CurrentIndex = 0;
            cache.Clear();
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
