using Baubit.Traceability;
using FluentResults;

namespace Baubit.Observation
{
    public interface ISubscriber<T> : IDisposable
    {
        public bool OnNext(T next);
        public bool OnError(Exception error);
        public bool OnCompleted();
    }

    public static class SubscriberExtensions
    {
        //public static Result OnNextOrError<T>(this ISubscriber<T> subscriber, T next)
        //{
        //    try
        //    {
        //        return subscriber.OnNext(next).ThrowIfFailed();
        //    }
        //    catch(Exception exp)
        //    {
        //        return subscriber.OnError(exp);
        //    }
        //}

        public static bool OnNextOrError<T>(this ISubscriber<T> subscriber, T next)
        {
            try
            {
                return subscriber.OnNext(next);
            }
            catch (Exception exp)
            {
                return subscriber.OnError(exp);
            }
        }
    }
}
