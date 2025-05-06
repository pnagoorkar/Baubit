using Baubit.Traceability;
using Baubit.Traceability.Exceptions;
using FluentResults;

namespace Baubit.Test.Traceability.Result
{
    public class Test
    {
        [Fact]
        public void CanThrowExceptionOnFailedResults()
        {
            var result = FluentResults.Result.Try(() => FluentResults.Result.Fail(string.Empty).ThrowIfFailed());
            Assert.True(result.IsFailed);
            Assert.Single(result.Reasons);
            Assert.IsType<ExceptionalError>(result.Reasons.First());
            Assert.IsType<FailedOperationException>(result.Reasons.OfType<ExceptionalError>().First().Exception);
        }
    }
}
