using Baubit.Traceability.Exceptions;
using FluentResults;

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

        public static TResult ThrowIfFailed<TResult>(this TResult result) where TResult : IResultBase
        {
            if (result.IsFailed) throw new FailedOperationException(result);
            return result;
        }

        public static async Task<TResult> ThrowIfFailed<TResult>(this Task<TResult> result) where TResult : IResultBase
        {
            return (await result).ThrowIfFailed();
        }

        public static TResult AddSuccessIfPassed<TResult, T>(this TResult result, params ISuccess[] successes) where TResult : Result<T>
        {
            return result.AddSuccessIfPassed((r, s) => r.WithSuccesses(s));
        }

        public static TResult AddSuccessIfPassed<TResult>(this TResult result,
                                                          Action<TResult, IEnumerable<ISuccess>> additionHandler,
                                                          params ISuccess[] successes) where TResult : IResultBase
        {
            if (result.IsSuccess) additionHandler(result, successes);
            return result;
        }

        public static Result AddReasonIfFailed(this Result result, params IReason[] reasons)
        {
            return result.AddReasonIfFailed((res, reas) => res.WithReasons(reasons), reasons);
        }

        public static TResult AddReasonIfFailed<TResult, T>(this TResult result, params IReason[] reasons) where TResult : Result<T>
        {
            return result.AddReasonIfFailed((res, reas) => res.WithReasons(reasons), reasons);
        }

        public static TResult AddReasonIfFailed<TResult>(this TResult result,
                                                          Action<TResult, IEnumerable<IReason>> additionHandler,
                                                          params IReason[] reasons) where TResult : IResultBase
        {
            if (result.IsFailed) additionHandler(result, reasons);
            return result;
        }

        public static TResult AddErrorIfFailed<TResult, TError>(this TResult result, params TError[] errors) where TResult : IResultBase where TError : IError
        {
            if (result.IsSuccess) result.Errors.AddRange(errors.Cast<IError>());
            return result;
        }

        public static Result<List<IReason>> GetNonErrors<TResult>(this TResult result) where TResult : IResultBase
        {
            var reasons = new List<IReason>();
            result.GetNonErrors(reasons);
            return reasons;
        }
        public static TResult GetNonErrors<TResult>(this TResult result, List<IReason> reasons) where TResult : IResultBase
        {
            result.UnwrapReasons().Bind(reasons => Result.Try(() => reasons.AddRange(reasons.Where(reas => reas is not IError).ToList())));
            return result;
        }

        public static Result<List<IReason>> UnwrapReasons<TResult>(this TResult result) where TResult : IResultBase
        {
            var reasons = new List<IReason>();
            result.UnwrapReasons(reasons);
            return reasons;
        }
        public static TResult UnwrapReasons<TResult>(this TResult result, List<IReason> reasons) where TResult : IResultBase
        {
            if (reasons == null) reasons = new List<IReason>();

            foreach (var reason in result.Reasons)
            {
                if (reason is ExceptionalError expErr && expErr.Exception is FailedOperationException failedOpExp)
                {
                    reasons.AddRange(failedOpExp.Result.Reasons);
                    failedOpExp.Result.UnwrapReasons(reasons);
                }
                else
                {
                    reasons.Add(reason);
                }
            }

            return result;
        }
    }
}
