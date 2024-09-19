
using Xunit.Abstractions;

namespace Baubit.Test.Store.Operations.Search
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.Search))]
    public class Test
    {
        private ITestOutputHelper testOutputHelper;
        public Test(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async void Works()
        {
        }
    }
}
