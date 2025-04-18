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

        public static TResult AddSuccessIfPassed<TResult>(this TResult result,
                                                          Action<TResult, IEnumerable<ISuccess>> additionHandler,
                                                          params ISuccess[] successes) where TResult : IResultBase
        {
            if (result.IsSuccess) additionHandler(result, successes);
            return result;
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

        public static List<IReason> GetNonErrors(this List<IReason> reasons) => reasons.Where(reason => reason is not IError).ToList();

        public static TResult UnwrapReasons<TResult>(this TResult result, List<IReason> reasons) where TResult : IResultBase
        {
            if (reasons == null) reasons = new List<IReason>();

            foreach (var error in result.Errors)
            {
                if (error is ExceptionalError expErr && expErr.Exception is FailedOperationException failedOpExp)
                {
                    reasons.AddRange(failedOpExp.Result.Reasons);
                    failedOpExp.Result.UnwrapReasons(reasons);
                }
            }

            return result;
        }
    }
}
