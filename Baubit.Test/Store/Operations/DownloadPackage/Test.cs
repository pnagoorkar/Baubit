using FluentResults.Extensions;
using System.Reflection;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Baubit.Test.Store.Operations.DownloadPackage
{
    public class Test
    {
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
            Assert.True(File.Exists(downloadResult.Value.DllFile));
        }
    }
}
