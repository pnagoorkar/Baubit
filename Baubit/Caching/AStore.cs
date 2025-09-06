using FluentResults;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching
{
    public abstract class AStore<TValue> : IStore<TValue>
    {
        public bool Uncapped { get => !TargetCapacity.HasValue; }
        public long? MinCapacity { get; init; } = null;
        public long? MaxCapacity { get; init; } = null;
        public long? TargetCapacity { get; private set; } = null;
        public long? CurrentCapacity { get => Uncapped ? null : Math.Max(0, TargetCapacity!.Value - GetCount()!.Value); }
        public bool HasCapacity { get => Uncapped || CurrentCapacity > 0; }

        public abstract long? HeadId { get; }

        public abstract long? TailId { get; }

        private ILogger<AStore<TValue>> _logger;
        private bool disposedValue;

        public AStore(long? minCap,
                         long? maxCap,
                         ILoggerFactory loggerFactory)
        {
            TargetCapacity = MinCapacity = minCap;
            MaxCapacity = maxCap;
            _logger = loggerFactory.CreateLogger<AStore<TValue>>();
        }

        public abstract bool Add(IEntry<TValue> entry);

        public abstract bool Add(TValue value, out IEntry<TValue>? entry);

        public bool AddCapacity(int additionalCapacity)
        {
            if (Uncapped) return true;
            TargetCapacity = Math.Min(MaxCapacity!.Value, TargetCapacity!.Value + additionalCapacity);
            return true;
        }

        public abstract bool Clear();

        public bool CutCapacity(int cap)
        {
            if (Uncapped) return true;
            TargetCapacity = Math.Max(MinCapacity!.Value, TargetCapacity!.Value - cap);
            return true;
        }

        private long? GetCount()
        {
            return GetCount(out var count) ? count : null;
        }

        public abstract bool GetCount(out long count);

        public abstract bool GetEntryOrDefault(long? id, out IEntry<TValue>? entry);

        public abstract bool GetValueOrDefault(long? id, out TValue? value);

        public abstract bool Remove(long id, out IEntry<TValue>? entry);

        public abstract bool Update(IEntry<TValue> entry);

        public abstract bool Update(long id, TValue value);

        protected abstract void DisposeInternal();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeInternal();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
