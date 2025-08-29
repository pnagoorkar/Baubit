using FluentResults;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching
{
    public abstract class ADataStore<TValue> : IDataStore<TValue>
    {
        public bool Uncapped { get => !TargetCapacity.HasValue; }
        public long? MinCapacity { get; init; } = null;
        public long? MaxCapacity { get; init; } = null;
        public long? TargetCapacity { get; private set; } = null;
        public long? CurrentCapacity { get => Uncapped ? null : Math.Max(0, TargetCapacity!.Value - GetCount().Value); }
        public bool HasCapacity { get => CurrentCapacity > 0; }

        public abstract long? HeadId { get; }

        public abstract long? TailId { get; }

        private ILogger<ADataStore<TValue>> _logger;
        private bool disposedValue;

        public ADataStore(long? minCap,
                         long? maxCap,
                         ILoggerFactory loggerFactory)
        {
            TargetCapacity = MinCapacity = minCap;
            MaxCapacity = maxCap;
            _logger = loggerFactory.CreateLogger<ADataStore<TValue>>();
        }

        public Result AddCapacity(int additionalCapacity)
        {
            if (Uncapped) return Result.Ok();
            return Result.Try(() =>
            {
                TargetCapacity = Math.Min(MaxCapacity!.Value, TargetCapacity!.Value + additionalCapacity);
            });
        }

        public Result CutCapacity(int cap)
        {
            if (Uncapped) return Result.Ok();
            return Result.Try(() =>
            {
                TargetCapacity = Math.Max(MinCapacity!.Value, TargetCapacity!.Value - cap);
            });
        }

        public abstract Result Add(IEntry<TValue> entry);

        public abstract Result<IEntry<TValue>> Add(TValue value);

        public abstract Result Clear();

        public abstract Result<long> GetCount();

        public abstract Result<IEntry<TValue>?> GetEntryOrDefault(long? id);

        public abstract Result<TValue?> GetValueOrDefault(long? id);

        public abstract Result<IEntry<TValue>?> Remove(long id);

        public abstract Result<IEntry<TValue>> Update(IEntry<TValue> entry);

        public abstract Result<IEntry<TValue>> Update(long id, TValue value);

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
