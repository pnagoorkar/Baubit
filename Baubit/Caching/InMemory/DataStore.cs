using Baubit.Caching.Reasons;
using Baubit.Traceability;
using FluentResults;
using System.Collections;
using System.Collections.Concurrent;

namespace Baubit.Caching.InMemory
{
    public class DataStore<TValue> : IDisposable
    {
        public long MaxCapacity { get; init; } = 8192;
        public long MinCapacity { get; init; } = 0;
        public long TargetCapacity { get; private set; } = 100;
        public long CurrentCapacity { get => Math.Max(0, TargetCapacity - GetCount().Value); }
        public bool HasCapacity { get => CurrentCapacity > 0; }

        private long _seq;
        private bool disposedValue;
        private readonly Dictionary<long, IEntry<TValue>> _data = new();

        public Result<IEntry<TValue>> Add(TValue value)
        {
            return Result.Try(() => Interlocked.Increment(ref _seq))
                         .Bind(id => Result.Try(() => new Entry<TValue>(id, value)))
                         .Bind(entry => Add(entry).Bind(() => Result.Ok<IEntry<TValue>>(entry)));
        }

        public Result Add(IEntry<TValue> entry)
        {
            return Result.Try(() => _data.TryAdd(entry.Id, entry))
                         .Bind(addRes => Result.OkIf(addRes && _data.ContainsKey(entry.Id), nameof(Add)).AddReasonIfFailed(new EntryNotFound<TValue>(entry.Id)));
        }

        public Result<IEntry<TValue>?> Remove(long id)
        {
            IEntry<TValue> entry = default!;

            return Result.Try(() => _data.Remove(id, out entry!)).Bind(_ => Result.Ok<IEntry<TValue>?>(entry));
        }

        public Result<IEntry<TValue>> Update(IEntry<TValue> entry)
        {
            return Update(entry.Id, entry.Value);
        }

        public Result<IEntry<TValue>> Update(long id, TValue value)
        {
            return Result.Try(() => new Entry<TValue>(id, value))
                         .Bind(entry => Result.Try(() => _data[id] = entry))
                         .Bind(entry => Result.Ok<IEntry<TValue>>(entry));
        }

        //public Result<IEntry<TValue>?> GetEntryOrDefault(long? id)
        //{
        //    if (id == null) return Result.Ok(default(IEntry<TValue>)).WithReason(new IdIsNull());
        //    try
        //    {
        //        return Result.Ok(_data[id.Value]);
        //    }
        //    catch (KeyNotFoundException kExp)
        //    {
        //        return Result.Ok(default(IEntry<TValue>)).WithReason(new EntryNotFound<TValue>(id.Value));
        //    }
        //    catch (Exception exp)
        //    {
        //        return Result.Fail(new ExceptionalError(exp));
        //    }
        //}

        public Result<IEntry<TValue>?> GetEntryOrDefault(long? id)
        {
            if (id == null)
            {
                return Result.Ok(default(IEntry<TValue>)).WithReason(new IdIsNull());
            }
            else if (_data.TryGetValue(id.Value, out var val))
            {
                return Result.Ok<IEntry<TValue>?>(val);
            }
            else
            {
                return Result.Ok(default(IEntry<TValue>)).WithReason(new EntryNotFound<TValue>(id.Value));
            }
        }

        public Result<TValue?> GetValueOrDefault(long? id)
        {
            return GetEntryOrDefault(id).Bind(entry => Result.Ok<TValue?>(entry == null ? default(TValue) : entry.Value));
        }

        public Result Clear()
        {
            return Result.Try(() => _data.Clear());
        }

        public Result<long> GetCount()
        {
            return Result.Try(() => (long)_data.Count);
        }

        public Result AddCapacity(int additionalCapacity)
        {
            return Result.Try(() => 
            {
                TargetCapacity = Math.Min(MaxCapacity, TargetCapacity + additionalCapacity);
            });
        }

        public Result CutCapacity(int cap)
        {
            return Result.Try(() =>
            {
                TargetCapacity = Math.Max(MinCapacity, TargetCapacity - cap);
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _data.Clear();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
