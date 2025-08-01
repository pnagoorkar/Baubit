using Baubit.Traceability;
using FluentResults;

namespace Baubit.Observation
{
    public interface ISubscriber<T>
    {
        public Result OnNext(T next);
        public Result OnError(Exception error);
        public Result OnCompleted();
    }

    public static class SubscriberExtensions
    {
        public static Result OnNextOrError<T>(this ISubscriber<T> subscriber, T next)
        {
            try
            {
                return subscriber.OnNext(next).ThrowIfFailed();
            }
            catch(Exception exp)
            {
                return subscriber.OnError(exp);
            }
        }
    }
}
