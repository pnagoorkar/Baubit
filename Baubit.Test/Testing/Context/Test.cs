using Baubit.DI;
using Baubit.Reflection;
using Baubit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.Testing.Context
{
    public class Test
    {
        [Fact]
        public void CanLoadContextFromEmbeddedJsonResource()
        {
            var result = ComponentBuilder<Context>.CreateFromSourceAttribute()
                                                  .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton<Context>()))
                                                  .Bind(compBuilder => compBuilder.Build());
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
    [Source(EmbeddedJsonResources = ["Baubit.Test;Testing.Context.context.json"])]
    public class Context : IContext
    {
        public void Dispose()
        {

        }
    }
}
