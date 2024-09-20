using Baubit.Store;
using FluentResults.Extensions;
using System.Reflection;
using System.Runtime.Loader;
using Xunit.Abstractions;

namespace Baubit.Test.Store.Operations.LoadAssembly
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.LoadAssembly))]
    public class Test
    {
        private ITestOutputHelper testOutputHelper;
        public Test(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async void CanLoadAssembliesDynamically()
        {
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };

            var loadResult = await Baubit.FileSystem
                                             .Operations
                                             .DeleteDirectoryIfExistsAsync(new Baubit.FileSystem.DirectoryDeleteContext(Application.BaubitRootPath, true))
                                             .Bind(() => Baubit.Store
                                                               .Operations
                                                               .DownloadPackageAsync(new Baubit.Store.PackageDownloadContext(assemblyName,
                                                                                                                             Application.TargetFramework,
                                                                                                                             Application.BaubitRootPath, true)))
                                             .Bind(downloadResult => Baubit.Store.Operations.LoadAssemblyAsync(new AssemblyLoadingContext(downloadResult.Package, downloadResult.Registry, Application.TargetFramework, AssemblyLoadContext.Default)));

            Assert.True(loadResult.IsSuccess);
            Assert.NotNull(loadResult.Value);
            Assert.Contains(loadResult.Value.ExportedTypes, type => type.AssemblyQualifiedName!.Contains("Autofac.Configuration.ConfigurationModule"));
        }
    }
}
