using Baubit.Reflection;
using Baubit.Testing;

namespace Baubit.Test.Testing.Context
{
    public class Test
    {
        [Fact]
        public void CanLoadContextFromEmbeddedJsonResource()
        {
            var result = ObjectLoader.Load<Context>();
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
    [Source(EmbeddedJsonResources = ["Baubit.Test;Testing.Context.context.json"])]
    public class Context : IContext
    {

    }
}
