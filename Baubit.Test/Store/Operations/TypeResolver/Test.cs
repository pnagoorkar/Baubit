
using Xunit.Abstractions;

namespace Baubit.Test.Store.Operations.TypeResolver
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.TypeResolver))]
    public class Test
    {
        private ITestOutputHelper testOutputHelper;
        public Test(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0")]
        public async void CanResolveTypeFromAssemblyQualifiedName(string assemblyQualifiedName)
        {
            var result = await Baubit.Store.TypeResolver.ResolveTypeAsync(assemblyQualifiedName);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}
