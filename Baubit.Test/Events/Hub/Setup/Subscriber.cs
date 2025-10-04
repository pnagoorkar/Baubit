using Baubit.Observation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baubit.Test.Events.Hub.Setup
{
    public class Subscriber<T> : ISubscriber<T>
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private T _expectedLastItem;
        public Subscriber(T expectedLastItem)
        {
            _expectedLastItem = expectedLastItem;
        }
        public bool OnCompleted()
        {
            throw new NotImplementedException();
        }

        public bool OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public bool OnNext(T next)
        {
            return next.Equals(_expectedLastItem) ? _tcs.TrySetResult(true) : true;
        }

        public async Task<bool> AwaitLastItem(CancellationToken cancellationToken = default)
        {
            return await _tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
        }
    }
}
