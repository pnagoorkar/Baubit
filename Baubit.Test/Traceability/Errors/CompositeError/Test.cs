using Baubit.Traceability;
using Baubit.Traceability.Reasons;
using FluentResults;

namespace Baubit.Test.Traceability.Errors.CompositeError
{
    public class Test
    {
        [Fact]
        public void CompositeErrorCanBeStringified()
        {
            var result = Result.Try(() => { throw new Exception(""); return 0; });
            var errorString = result.WithReason(new MyReason()).CaptureAsError().ToString();
            Assert.False(string.IsNullOrEmpty(errorString));
        }
    }

    public class MyReason : AReason
    {
        public MyReason() : base("Some specific reason", default)
        {
        }
    }
}
