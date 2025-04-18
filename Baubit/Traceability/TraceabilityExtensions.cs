using Baubit.Traceability.Errors;
using Baubit.Traceability.Exceptions;
using FluentResults;
using System.Collections.Generic;

namespace Baubit.Traceability
{
    public static class TraceabilityExtensions
    {
        public static Result Dispose<TDisposable>(this IList<TDisposable> disposables) where TDisposable : IDisposable
        {
            return Result.Try(() =>
            {
                for (int i = 0; i < disposables.Count; i++)
                {
                    disposables[i].Dispose();
                }
            });
        }

        public static CompositeError<T> CaptureAsError<T>(this Result<T> result)
        {
            return new CompositeError<T>(result);
        }

        public static TResult ThrowIfFailed<TResult>(this TResult result) where TResult : IResultBase
        {
            if (result.IsFailed) throw new FailedOperationException(result);
            return result;
        }

        public static TResult AddSuccessIfPassed<TResult, TSuccess>(this TResult result, params TSuccess[] successes) where TResult : IResultBase where TSuccess : ISuccess
        {
            if (result.IsSuccess) result.Successes.AddRange(successes.Cast<ISuccess>());
            return result;
        }

        public static TResult AddReasonIfFailed<TResult, TReason>(this TResult result, params TReason[] reasons) where TResult : IResultBase where TReason : IReason
        {
            if (result.IsSuccess) result.Reasons.AddRange(reasons.Cast<IReason>());
            return result;
        }

        public static TResult AddErrorIfFailed<TResult, TError>(this TResult result, params TError[] errors) where TResult : IResultBase where TError : IError
        {
            if (result.IsSuccess) result.Errors.AddRange(errors.Cast<IError>());
            return result;
        }
    }
}
