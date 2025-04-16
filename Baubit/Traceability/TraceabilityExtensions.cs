using Baubit.Traceability.Errors;
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

        public static CompositeError<T> CaptureAsError<T>(this Result<T> result)
        {
            return new CompositeError<T>(result);
        }

        public static void ThrowIfFailed(this IResultBase result)
        {
            if (result.IsFailed) throw new Exception(result.ToString());
        }
    }
}
