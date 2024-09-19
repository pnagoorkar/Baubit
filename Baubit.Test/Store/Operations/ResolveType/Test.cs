
using FluentResults.Extensions;

namespace Baubit.Test.Store.Operations.ResolveType
{
    public class Test
    {
        [Theory]
        [InlineData("Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0")]
        public async void CanResolveTypeFromAssemblyQualifiedName(string assemblyQualifiedName)
        {
            var result = await Baubit.FileSystem.Operations.DeleteDirectoryIfExistsAsync(new Baubit.FileSystem.DirectoryDeleteContext(Application.BaubitRootPath, true))
                             .Bind(() => Baubit.Store.Operations.ResolveTypeAsync(new Baubit.Store.TypeResolutionContext(assemblyQualifiedName)));
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}
