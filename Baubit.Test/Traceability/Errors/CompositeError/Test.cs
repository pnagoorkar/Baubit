namespace Baubit.Test.Traceability.Errors.CompositeError
{
    public class Test
    {
        [Fact]
        public void CompositeErrorCanBeStringified()
        {
            var compositeError = new Baubit.Traceability.Errors.CompositeError<string>();
            Assert.False(string.IsNullOrEmpty(compositeError.ToString()));
        }
    }
}
