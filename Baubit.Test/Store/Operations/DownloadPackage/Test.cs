using Baubit.Store;
using FluentResults.Extensions;
using System.Reflection;
using Xunit.Abstractions;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Baubit.Test.Store.Operations.DownloadPackage
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.DownloadPackage))]
    public class Test
    {
        private ITestOutputHelper testOutputHelper;
        public Test(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async void CanDownloadPackages()
        {
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };

            var downloadResult = await Baubit.FileSystem
                                             .Operations
                                             .DeleteDirectoryIfExistsAsync(new Baubit.FileSystem.DirectoryDeleteContext(Application.BaubitRootPath, true))
                                             .Bind(() => Baubit.Store
                                                               .Operations
                                                               .DownloadPackageAsync(new Baubit.Store.PackageDownloadContext(assemblyName,
                                                                                                                             Application.TargetFramework,
                                                                                                                             Application.BaubitRootPath, false)));

            Assert.True(downloadResult.IsSuccess);
            Assert.NotNull(downloadResult.Value.Package);
            Assert.True(File.Exists(downloadResult.Value.Package.DllFile));
        }
    }
}
